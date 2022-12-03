using AnyWichWay.Entities;
using AnyWichWay.Enums;
using Newtonsoft.Json.Linq;
using SqlBulkTools;
using System.Data.SqlClient;

Dictionary<Tastes, int> TasteValueHolder = new();
foreach (Tastes taste in Enum.GetValues<Tastes>())
    TasteValueHolder[taste] = 0;

Dictionary<Powers, int> PowerValueHolder = new();
foreach (Powers power in Enum.GetValues<Powers>())
    PowerValueHolder[power] = 0;

Dictionary<Types, int> TypesValueHolder = new();
foreach (Types type in Enum.GetValues<Types>())
    TypesValueHolder[type] = 0;


Console.Write($"Preparing Database...");

string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AnyWichWay;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
using SqlConnection sqlConnection = new(connectionString);
using SqlCommand sqlCommand = new("TRUNCATE TABLE [dbo].[Sandwiches];", sqlConnection);
await sqlConnection.OpenAsync();
await sqlCommand.ExecuteNonQueryAsync();
await sqlConnection.CloseAsync();

Console.WriteLine($" Done!");
Console.WriteLine();

Console.Write($"Importing Ingredients...");

IEnumerable<Filling> allFillings;
IEnumerable<Condiment> allCondiments;

(allFillings, allCondiments) = await ImportIngredients("Ingredients.json");

Console.WriteLine($" Done!");
Console.WriteLine();


Console.Write($"Theorizing Combinations...");

IEnumerable<List<Filling>> fillingCombinations = GenerateCombinations(allFillings.ToList(), 3).Where(fillingCombination => fillingCombination.Any(filling => filling.Name != Fillings.None));
IEnumerable<List<Condiment>> condimentCombinations = GenerateCombinations(allCondiments.ToList(), 2).Where(condimentCombination => condimentCombination.Any(condiment => condiment.Name != Condiments.None));
long possibleCombinations = fillingCombinations.Count() * condimentCombinations.Count();

Console.WriteLine($" Done!");
Console.WriteLine($"{possibleCombinations} Possible Combinations...");
Console.WriteLine();


Console.WriteLine($"Making sandwiches...");

int sandwichesChecked = 0;
Dictionary<string, Sandwich> sandwiches = new();
foreach((Sandwich sandwich, List<Filling> fillingCombination, List<Condiment> condimentCombination, MealPower mealPower1, MealPower mealPower2, MealPower mealPower3) 
    in MakeSandwiches(fillingCombinations, condimentCombinations))
{
    sandwichesChecked++; 
    if (sandwichesChecked % 10000 == 0)
        Console.Write($"\r{sandwiches.Count} sandwiches made with {sandwichesChecked}/{possibleCombinations} possible ones checked...");

    if (sandwiches.TryGetValue(sandwich.Key, out Sandwich? currentSandwich) && 
        currentSandwich != null &&
        sandwich.Cost >= currentSandwich.Cost)
        continue;    

    sandwiches[sandwich.Key] = FinalizeSandwich(sandwich, fillingCombination, condimentCombination, mealPower1, mealPower2, mealPower3);
}

Console.WriteLine($" Done!");


Console.Write($"Saving menu...");

BulkOperations bulkOperations = new(connectionString);

bulkOperations.Setup<Sandwich>(x => x.ForCollection(sandwiches.Values))
    .WithTable("Sandwiches")
    .AddAllColumns()
    .BulkInsert();

await bulkOperations.CommitTransactionAsync();

Console.WriteLine($" Done!");


Sandwich FinalizeSandwich(Sandwich sandwich, List<Filling> fillingCombination, List<Condiment> condimentCombination, MealPower mealPower1, MealPower mealPower2, MealPower mealPower3)
{
    sandwich.Taste = CalculateTaste(fillingCombination, condimentCombination).ToString();
    sandwich.Fillings = string.Join(", ", fillingCombination.Where(filling => filling.Name != Fillings.None).Select(filling => filling.Name));
    sandwich.Condiments = string.Join(", ", condimentCombination.Where(condiment => condiment.Name != Condiments.None).Select(condiment => condiment.Name));
    sandwich.MealPower1 = GenerateMealPowerName(mealPower1);
    sandwich.MealPower2 = GenerateMealPowerName(mealPower2);
    sandwich.MealPower3 = GenerateMealPowerName(mealPower3);

    return sandwich;
}

string GenerateMealPowerName(MealPower mealPower)
{
    return $"{mealPower.Power}{(mealPower.Power != Powers.Egg ? $": {mealPower.Type}" : string.Empty)} - Lv. {mealPower.Level}";
}

async Task<(IEnumerable<Filling> Fillings, IEnumerable<Condiment> Condiments)> ImportIngredients(string ingredientsJsonPath)
{
    List<Filling> AllFillings = new();
    List<Condiment> AllCondiments = new();

    string ingredientsText = await File.ReadAllTextAsync(ingredientsJsonPath);

    JObject ingredients = JObject.Parse(ingredientsText);

    foreach (JToken filling in ingredients["Fillings"]!)
    {
        Filling newFilling = new()
        {
            Name = Enum.Parse<Fillings>(filling["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
            Shop = Enum.Parse<Shops>(filling["Shop"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
            Cost = filling["Cost"]?.Value<int>() ?? throw new ArgumentException($"Could not parse cost of {filling["Name"]}!")
        };

        foreach (JToken tasteValue in filling["Taste"]!)
        {
            newFilling.TasteValues.Add(new TasteValue
            {
                Name = Enum.Parse<Tastes>(tasteValue["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
                Value = tasteValue["Value"]?.Value<int>() ?? throw new ArgumentException($"Could not taste value of {filling["Name"]} and {tasteValue["Name"]}!")
            });
        }

        foreach (JToken powerValue in filling["Power"]!)
        {
            newFilling.PowerValues.Add(new PowerValue
            {
                Name = Enum.Parse<Powers>(powerValue["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
                Value = powerValue["Value"]?.Value<int>() ?? throw new ArgumentException($"Could not power value of {filling["Name"]} and {powerValue["Name"]}!")
            });
        }

        foreach (JToken typeValue in filling["Type"]!)
        {
            newFilling.TypeValues.Add(new TypeValue
            {
                Name = Enum.Parse<Types>(typeValue["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
                Value = typeValue["Value"]?.Value<int>() ?? throw new ArgumentException($"Could not type value of {filling["Name"]} and {typeValue["Name"]}!")
            });
        }

        AllFillings.Add(newFilling);
    }

    foreach (JToken condiment in ingredients["Condiments"]!)
    {
        Condiment newCondiment = new()
        {
            Name = Enum.Parse<Condiments>(condiment["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
            Shop = Enum.Parse<Shops>(condiment["Shop"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
            Cost = condiment["Cost"]?.Value<int>() ?? throw new ArgumentException($"Could not parse cost of {condiment["Name"]}!")
        };

        foreach (JToken tasteValue in condiment["Taste"]!)
        {
            newCondiment.TasteValues.Add(new TasteValue
            {
                Name = Enum.Parse<Tastes>(tasteValue["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
                Value = tasteValue["Value"]?.Value<int>() ?? throw new ArgumentException($"Could not taste value of {condiment["Name"]} and {tasteValue["Name"]}!")
            });
        }

        foreach (JToken powerValue in condiment["Power"]!)
        {
            newCondiment.PowerValues.Add(new PowerValue
            {
                Name = Enum.Parse<Powers>(powerValue["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
                Value = powerValue["Value"]?.Value<int>() ?? throw new ArgumentException($"Could not power value of {condiment["Name"]} and {powerValue["Name"]}!")
            });
        }

        foreach (JToken typeValue in condiment["Type"]!)
        {
            newCondiment.TypeValues.Add(new TypeValue
            {
                Name = Enum.Parse<Types>(typeValue["Name"]?.Value<string>()?.Replace(" ", "") ?? string.Empty),
                Value = typeValue["Value"]?.Value<int>() ?? throw new ArgumentException($"Could not type value of {condiment["Name"]} and {typeValue["Name"]}!")
            });
        }

        AllCondiments.Add(newCondiment);
    }

    return (AllFillings, AllCondiments);
}

IEnumerable<(Sandwich, List<Filling>, List<Condiment>, MealPower, MealPower, MealPower)> 
    MakeSandwiches(IEnumerable<List<Filling>> fillingCombinations, IEnumerable<List<Condiment>> condimentCombinations)
{
    foreach(List<Filling> fillingCombination in fillingCombinations)
    {
        foreach (List<Condiment> condimentCombination in condimentCombinations)
        {
            Sandwich sandwich = new()
            {
                Cost = fillingCombination.Sum(filling => filling.Cost) + condimentCombination.Sum(condiment => condiment.Cost)
            };
            (MealPower mealPower1, MealPower mealPower2, MealPower mealPower3) = CalculateMealPowers(fillingCombination, condimentCombination);
            
            sandwich.Key = GenerateSandwichKey(mealPower1, mealPower2, mealPower3);
                
            yield return (sandwich, fillingCombination, condimentCombination, mealPower1!, mealPower2!, mealPower3!);
        }
    }
}

static List<List<T>> GenerateCombinations<T>(List<T> combinationList, int k)
{
    List<List<T>> combinations = new();

    if (k == 0)
    {
        List<T> emptyCombination = new();
        combinations.Add(emptyCombination);

        return combinations;
    }

    if (combinationList.Count == 0)
    {
        return combinations;
    }

    T head = combinationList[0];
    List<T> copiedCombinationList = new(combinationList);

    List<List<T>> subcombinations = GenerateCombinations(copiedCombinationList, k - 1);

    foreach (List<T> subcombination in subcombinations)
    {
        subcombination.Insert(0, head);
        combinations.Add(subcombination);
    }

    combinationList.RemoveAt(0);
    combinations.AddRange(GenerateCombinations(combinationList, k));

    return combinations;
}

Tastes CalculateTaste(List<Filling> fillingCombination, List<Condiment> condimentCombination)
{
    Dictionary<Tastes, int> tastes = TasteValueHolder.ToDictionary(taste => taste.Key, taste => taste.Value); 

    foreach(Filling filling in fillingCombination)
    {
        foreach (TasteValue tasteValue in filling.TasteValues)
        {
            tastes[tasteValue.Name] += tasteValue.Value;                   
        }
    }

    foreach (Condiment condiment in condimentCombination)
    {
        foreach (TasteValue tasteValue in condiment.TasteValues)
        {
            tastes[tasteValue.Name] += tasteValue.Value;
        }
    }

    return tastes.MaxBy(taste => taste.Value).Key;
}

(MealPower, MealPower, MealPower) CalculateMealPowers(List<Filling> fillingCombination, List<Condiment> condimentCombination)
{
    Dictionary<Powers, int> powers = PowerValueHolder.ToDictionary(power => power.Key, power => power.Value);
    Dictionary<Types, int> types = TypesValueHolder.ToDictionary(type => type.Key, type => type.Value);

    foreach (Filling filling in fillingCombination)
    {
        foreach (PowerValue powerValue in filling.PowerValues)
        {
            powers[powerValue.Name] += powerValue.Value;
        }
        foreach (TypeValue typeValue in filling.TypeValues)
        {
            types[typeValue.Name] += typeValue.Value;
        }
    }

    foreach (Condiment condiment in condimentCombination)
    {
        foreach (PowerValue powerValue in condiment.PowerValues)
        {
            powers[powerValue.Name] += powerValue.Value;
        }
        foreach (TypeValue typeValue in condiment.TypeValues)
        {
            types[typeValue.Name] += typeValue.Value;
        }
    }

    Queue<(Powers, int)> orderedPowers = new Queue<(Powers, int)>(powers.OrderByDescending(power => power.Value).Select(power => (power.Key, power.Value)));
    Queue<(Types, int)> orderedTypes = new Queue<(Types, int)>(types.OrderByDescending(type => type.Value).Select(type => (type.Key, type.Value)));
    
    (Powers power1, int powerValue1) = orderedPowers.Dequeue();
    Types type1 = Types.None;
    int typeValue1 = 0;
    if (power1 != Powers.Egg)
        (type1, typeValue1) = orderedTypes.Dequeue();

    (Powers power2, int powerValue2) = orderedPowers.Dequeue();
    Types type2 = Types.None;
    int typeValue2 = 0;
    if (power2 != Powers.Egg)
        (type2, typeValue2) = orderedTypes.Dequeue();

    (Powers power3, int powerValue3) = orderedPowers.Dequeue();
    Types type3 = Types.None;
    int typeValue3 = 0;
    if (power3 != Powers.Egg)
        (type3, typeValue3) = orderedTypes.Dequeue();

    return (new MealPower {
        Power = power1,
        Type = type1,
        Level = CalculateLevel(powerValue1 + typeValue1)
    },
    new MealPower
    {
        Power = power2,
        Type = type2,
        Level = CalculateLevel(powerValue1 + typeValue1)
    },
    new MealPower
    {
        Power = power3,
        Type = type3,
        Level = CalculateLevel(powerValue1 + typeValue1)
    });
}

int CalculateLevel(int value)
{
    if (value < 100)
        return 1;
    else if (value < 2000)
        return 2;
    else
        return 3;
}

string GenerateSandwichKey(MealPower? mealPower1, MealPower? mealPower2, MealPower mealPower3)
{
    int mealPower1Key = (int)(mealPower1?.Power ?? 0) | (int)(mealPower1?.Type ?? 0);
    int mealPower2Key = (int)(mealPower2?.Power ?? 0) | (int)(mealPower2?.Type ?? 0);
    int mealPower3Key = (int)(mealPower3?.Power ?? 0) | (int)(mealPower3?.Type ?? 0);

    Queue<int> mealPowers = new(new int[] { mealPower1Key, mealPower2Key, mealPower3Key }.Order());

    return $"{mealPowers.Dequeue()}{mealPowers.Dequeue()}{mealPowers.Dequeue()}";
}