using AnyWichWay.Entities;
using AnyWichWay.Enums;
using Combinatorics.Collections;
using Newtonsoft.Json.Linq;
using SqlBulkTools;
using System.Collections.Concurrent;
using System.Data.SqlClient;

const int MaxFillingAmount = 6;
const int MaxCondimentAmount = 4;
const int parallelProcesses = 20;
const int updateMilliseconds = 500;
const string connectionString = "Data Source=MATT-PC;Initial Catalog=AnyWichWay;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

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

List<IEnumerable<Filling>> fillingCombinations = new();
for (int fillingCount = 1 ; fillingCount <= MaxFillingAmount; fillingCount++)
{
    fillingCombinations.AddRange(new Combinations<Filling>(allFillings.ToList(), fillingCount, GenerateOption.WithRepetition));
}

List<IEnumerable<Condiment>> condimentCombinations = new();
for (int condimentCount = 1; condimentCount <= MaxCondimentAmount; condimentCount++)
{
    condimentCombinations.AddRange(new Combinations<Condiment>(allCondiments.ToList(), condimentCount, GenerateOption.WithRepetition));
}

long possibleCombinations = (long)fillingCombinations.Count() * (long)condimentCombinations.Count();

Console.WriteLine($" Done!");
Console.WriteLine($"{possibleCombinations} Possible Combinations...");
Console.WriteLine();


Console.WriteLine($"Making sandwiches...");

int sandwichesChecked = 0;
ConcurrentDictionary<int, Sandwich> sandwiches = new();
DateTime timeStarted = DateTime.Now;

IEnumerable<IEnumerable<IEnumerable<Filling>>> splitFillingCombinations = Section(fillingCombinations, fillingCombinations.Count / parallelProcesses + 1);
List<Task> sandwichTasks = new();

foreach (IEnumerable<IEnumerable<Filling>> fillingCombinationsSplit in splitFillingCombinations)
{
    sandwichTasks.Add(Task.Run(() =>
    {
        foreach (Sandwich sandwich in MakeSandwiches(fillingCombinationsSplit, condimentCombinations))
        {
            Interlocked.Increment(ref sandwichesChecked);
            sandwiches.AddOrUpdate(sandwich.Key, sandwich, (int key, Sandwich currentSandwich) => sandwich.Cost < currentSandwich.Cost ? sandwich : currentSandwich);
        }
    }));
}

sandwichTasks.Add(Task.Run(async () =>
{
    while (sandwichesChecked < possibleCombinations)
    {
        await Task.Delay(updateMilliseconds);
        if (sandwichesChecked > 0)
        {
            Console.Write($"\r{sandwiches.Count} sandwiches made with {sandwichesChecked}/{possibleCombinations} ({((double)sandwichesChecked / possibleCombinations).ToString("0.00%")}) possible ones checked, with around {(((DateTime.Now - timeStarted) / sandwichesChecked) * (possibleCombinations - sandwichesChecked)).ToString(@"dd\.hh\:mm\:ss")} left...");
        }
    }
}));

Task.WaitAll(sandwichTasks.ToArray());

Console.WriteLine($" Done!"); 
Console.WriteLine();


Console.Write($"Saving menu...");

BulkOperations bulkOperations = new(connectionString);

bulkOperations.Setup<Sandwich>(x => x.ForCollection(sandwiches.Values))
    .WithTable("Sandwiches")
    .AddAllColumns()
    .BulkInsert();

await bulkOperations.CommitTransactionAsync();

Console.WriteLine($" Done!");


Console.WriteLine($"AnyWichWay complete, press any key to close...");
Console.WriteLine();
Console.ReadKey();

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
            Count = filling["Count"]?.Value<int>() ?? throw new ArgumentException($"Could not parse count of {filling["Name"]}!"),
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

static IEnumerable<IEnumerable<T>> Section<T>(IEnumerable<T> source, int length)
{
    if (length <= 0)
        throw new ArgumentOutOfRangeException(nameof(length));

    var section = new List<T>(length);

    foreach (var item in source)
    {
        section.Add(item);

        if (section.Count == length)
        {
            yield return section.AsReadOnly();
            section = new List<T>(length);
        }
    }

    if (section.Count > 0)
        yield return section.AsReadOnly();
}


IEnumerable<Sandwich> MakeSandwiches(IEnumerable<IEnumerable<Filling>> fillingCombinations, IEnumerable<IEnumerable<Condiment>> condimentCombinations)
{
    foreach (IEnumerable<Filling> fillingCombination in fillingCombinations)
    {
        if (fillingCombination.GroupBy(filling => filling.Name).Any(group => group.Sum(filling => filling.Count) > 12))
            continue;

        foreach (IEnumerable<Condiment> condimentCombination in condimentCombinations)
        {
            TallyValues(TasteValueHolder, PowerValueHolder, TypesValueHolder, fillingCombination, condimentCombination, out Dictionary<Tastes, int> tastes, out Dictionary<Powers, int> powers, out Dictionary<Types, int> types);

            CalculateMealPowers(tastes, powers, types, out string taste, out MealPower mealPower1, out MealPower mealPower2, out MealPower mealPower3);

            yield return new()
            {
                Key = GenerateSandwichKey(mealPower1, mealPower2, mealPower3),
                Fillings = string.Join(", ", fillingCombination.Select(filling => filling.Name)),
                Condiments = string.Join(", ", condimentCombination.Select(condiment => condiment.Name)),
                Cost = fillingCombination.Sum(filling => filling.Cost) + condimentCombination.Sum(condiment => condiment.Cost),
                MealPower1 = GenerateMealPowerName(mealPower1),
                MealPower2 = GenerateMealPowerName(mealPower2),
                MealPower3 = GenerateMealPowerName(mealPower3),
                Taste = taste,
                Sweet = tastes[Tastes.Sweet],
                Salty = tastes[Tastes.Salty],
                Sour = tastes[Tastes.Sour],
                Bitter = tastes[Tastes.Bitter],
                Hot = tastes[Tastes.Hot],
                Egg = powers[Powers.Egg],
                Catching = powers[Powers.Catching],
                Exp = powers[Powers.Exp],
                Raid = powers[Powers.Raid],
                ItemDrop = powers[Powers.ItemDrop],
                Humungo = powers[Powers.Humungo],
                Teensy = powers[Powers.Teensy],
                Encounter = powers[Powers.Encounter],
                Title = powers[Powers.Title],
                Sparkling = powers[Powers.Sparkling],
                Normal = types[Types.Normal],
                Fighting = types[Types.Fighting],
                Flying = types[Types.Flying],
                Poison = types[Types.Poison],
                Ground = types[Types.Ground],
                Rock = types[Types.Rock],
                Bug = types[Types.Bug],
                Ghost = types[Types.Ghost],
                Steel = types[Types.Steel],
                Fire = types[Types.Fire],
                Water = types[Types.Water],
                Grass = types[Types.Grass],
                Electric = types[Types.Electric],
                Psychic = types[Types.Psychic],
                Ice = types[Types.Ice],
                Dragon = types[Types.Dragon],
                Dark = types[Types.Dark],
                Fairy = types[Types.Fairy],
            };
        }
    }
}

static void TallyValues(Dictionary<Tastes, int> TasteValueHolder, Dictionary<Powers, int> PowerValueHolder, Dictionary<Types, int> TypesValueHolder, IEnumerable<Filling> fillingCombination, IEnumerable<Condiment> condimentCombination, out Dictionary<Tastes, int> tastes, out Dictionary<Powers, int> powers, out Dictionary<Types, int> types)
{
    tastes = TasteValueHolder.ToDictionary(taste => taste.Key, taste => taste.Value);
    powers = PowerValueHolder.ToDictionary(power => power.Key, power => power.Value);
    types = TypesValueHolder.ToDictionary(type => type.Key, type => type.Value);

    foreach (Filling filling in fillingCombination)
    {
        foreach (TasteValue tasteValue in filling.TasteValues)
        {
            tastes[tasteValue.Name] += tasteValue.Value * filling.Count;
        }
        foreach (PowerValue powerValue in filling.PowerValues)
        {
            powers[powerValue.Name] += powerValue.Value * filling.Count;
        }
        foreach (TypeValue typeValue in filling.TypeValues)
        {
            types[typeValue.Name] += typeValue.Value * filling.Count;
        }
    }

    foreach (Condiment condiment in condimentCombination)
    {
        foreach (TasteValue tasteValue in condiment.TasteValues)
        {
            tastes[tasteValue.Name] += tasteValue.Value;
        }
        foreach (PowerValue powerValue in condiment.PowerValues)
        {
            powers[powerValue.Name] += powerValue.Value;
        }
        foreach (TypeValue typeValue in condiment.TypeValues)
        {
            types[typeValue.Name] += typeValue.Value;
        }
    }
}

void CalculateMealPowers(Dictionary<Tastes, int> tastes, Dictionary<Powers, int> powers, Dictionary<Types, int> types, out string taste, out MealPower mealPower1, out MealPower mealPower2, out MealPower mealPower3)
{
    Queue<(Types, int)> orderedTypes = new(types.OrderByDescending(type => type.Value).ThenBy(type => type.Key).Select(type => (type.Key, type.Value)));
    IEnumerable<Tastes> orderedTastes = tastes.OrderByDescending(taste => taste.Value).ThenBy(taste => taste.Key).Select(taste => taste.Key);

    taste = GetTaste(orderedTastes, ref powers); 
    Queue<(Powers, int)> orderedPowers = new(powers.OrderByDescending(power => power.Value).ThenBy(power => power.Key).Select(power => (power.Key, power.Value)));

    (Powers power1, int powerValue1) = orderedPowers.Dequeue();
    (Types type1, int typeValue1) = orderedTypes.Dequeue();

    (Powers power2, int powerValue2) = orderedPowers.Dequeue();
    (Types type2, int typeValue2) = orderedTypes.Dequeue();

    (Powers power3, int powerValue3) = orderedPowers.Dequeue();
    (Types type3, int typeValue3) = orderedTypes.Dequeue();

    (type1, type2, type3) = CalculateTypes(type1, typeValue1, powerValue2, type2, typeValue2, powerValue3, type3, typeValue3);
    (int level1, int level2, int level3) = CalculateLevels(typeValue1, typeValue2, typeValue3);

    mealPower1 = new()
    {
        Power = power1,
        Type = type1,
        Level = level1
    };
    mealPower2 = new()
    {
        Power = power2,
        Type = type2,
        Level = level2
    };
    mealPower3 = new()
    {
        Power = power3,
        Type = type3, 
        Level = level3
    };
}


string GetTaste(IEnumerable<Tastes> orderedTastes, ref Dictionary<Powers, int> powers)
{
    switch (orderedTastes.First())
    {
        case Tastes.Salty:
            if (orderedTastes.ElementAt(1) == Tastes.Bitter)
            {
                powers[Powers.Exp] += 100;
                return $"{nameof(Tastes.Salty)} & {nameof(Tastes.Bitter)}";
            }
            else
            {
                powers[Powers.Encounter] += 100;
                return nameof(Tastes.Salty);
            }

        case Tastes.Bitter:
            if (orderedTastes.ElementAt(1) == Tastes.Salty)
            {
                powers[Powers.Exp] += 100;
                return $"{nameof(Tastes.Bitter)} & {nameof(Tastes.Salty)}";
            }
            else
            {
                powers[Powers.ItemDrop] += 100;
                return nameof(Tastes.Bitter);
            }

        case Tastes.Sour:
            if (orderedTastes.ElementAt(1) == Tastes.Sweet)
            {
                powers[Powers.Catching] += 100;
                return $"{nameof(Tastes.Sour)} & {nameof(Tastes.Sweet)}";
            }
            else
            {
                powers[Powers.Teensy] += 100;
                return nameof(Tastes.Sour);
            }

        case Tastes.Hot:
            if (orderedTastes.ElementAt(1) == Tastes.Sweet)
            {
                powers[Powers.Raid] += 100;
                return $"{nameof(Tastes.Hot)} & {nameof(Tastes.Sweet)}";
            }
            else
            {
                powers[Powers.Humungo] += 100;
                return nameof(Tastes.Hot);
            }
             
        case Tastes.Sweet:
        default:
            if (orderedTastes.ElementAt(1) == Tastes.Sour)
            {
                powers[Powers.Catching] += 100;
                return $"{nameof(Tastes.Sweet)} & {nameof(Tastes.Sour)}";
            }
            else if (orderedTastes.ElementAt(1) == Tastes.Hot)
            {
                powers[Powers.Raid] += 100;
                return $"{nameof(Tastes.Sweet)} & {nameof(Tastes.Hot)}";
            } 
            else
            {
                powers[Powers.Egg] += 100;
                return nameof(Tastes.Sweet);
            }
    }
}

(int level1, int level2, int level3) CalculateLevels(int typeValue1, int typeValue2, int typeValue3)
{
    if (typeValue1 >= 460)
    {
        return (3, 3, 3);
    }
    else if (typeValue1 >= 380)
    {
        if (typeValue2 >= 380 && typeValue3 >= 380)
        {
            return (3, 3, 3);
        }
        else
        {
            return (3, 3, 2);
        }
    }
    else if (typeValue1 >= 281)
    {
        if (typeValue3 >= 180)
        {
            return (2, 2, 2);
        }
        else
        {
            return (2, 2, 1);
        }
    }
    else if (typeValue1 >= 180)
    {
        if (typeValue2 >= 180 && typeValue3 >= 180)
        {
            return (2, 2, 1);
        }
        else
        {
            return (2, 1, 1);
        }
    }
    else
    {
        return (1, 1, 1);
    }
}  

(Types type1, Types type2, Types type3) CalculateTypes(Types type1, int typeValue1, int powerValue2, Types type2, int typeValue2, int powerValue3, Types type3, int typeValue3)
{
    if (typeValue1 > 480)
    {
        return (type1, type1, type1);
    }
    else if (typeValue1 > 280)
    {
        return (type1, type1, type3);
    }
    else if (typeValue1 > 100)
    {
        int firstTwoDifference = typeValue1 - typeValue2;

        if (firstTwoDifference >= 100)
        {
            return (type1, type1, type3);
        }
        else if (firstTwoDifference >= 82)
        {
            return (type1, type3, type1);
        }
        else if (firstTwoDifference >= 72)
        {
            return (type1, type3, type2);
        }
    }
    else if(typeValue1 - typeValue2 >= 72)
    {
        if (powerValue2 > 60 && powerValue3 > 60)
        {
            if (Math.Abs(powerValue2 - powerValue3) <= 10)
            {
                return (type1, type3, type2);
            }
            else
            {
                return (type1, type3, type1);
            }
        }
        else
            return (type1, type3, type1);
    }

    return (type1, type3, type2);
}

string GenerateMealPowerName(MealPower mealPower)
{
    return $"{mealPower.Power}{(mealPower.Power != Powers.Egg ? $": {mealPower.Type}" : string.Empty)} - Lv. {mealPower.Level}";
}

int GenerateSandwichKey(MealPower mealPower1, MealPower mealPower2, MealPower mealPower3)
{
    int mealPower1Key = (int)mealPower1.Power + (int)mealPower1.Type * 10 + ((mealPower1.Level - 1) * 180);
    int mealPower2Key = (int)mealPower2.Power + (int)mealPower2.Type * 10 + ((mealPower2.Level - 1) * 180);
    int mealPower3Key = (int)mealPower3.Power + (int)mealPower3.Type * 10 + ((mealPower3.Level - 1) * 180);

    return mealPower1Key + (mealPower2Key * 1000) + (mealPower3Key * 1000000);
}
