using AnyWichWay.Entities;
using AnyWichWay.Enums;
using Microsoft.EntityFrameworkCore.Internal;
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
IEnumerable<List<Condiment>> condimentCombinations = GenerateCombinations(allCondiments.ToList(), 3).Where(condimentCombination => condimentCombination.Any(condiment => condiment.Name != Condiments.None));
long possibleCombinations = fillingCombinations.Count() * condimentCombinations.Count();

Console.WriteLine($" Done!");
Console.WriteLine($"{possibleCombinations} Possible Combinations...");
Console.WriteLine();


Console.WriteLine($"Making sandwiches...");

int sandwichesChecked = 0;
List<Sandwich> sandwiches = new();
foreach(Sandwich sandwich in MakeSandwiches(fillingCombinations, condimentCombinations))
{
    sandwiches.Add(sandwich);
    sandwichesChecked++;

    if (sandwichesChecked % 10000 == 0)
        Console.Write($"\r{sandwiches.Count} sandwiches made with {sandwichesChecked}/{possibleCombinations} possible ones checked...");

}

Console.WriteLine($" Done!");


Console.Write($"Saving menu...");

BulkOperations bulkOperations = new(connectionString);

bulkOperations.Setup<Sandwich>(x => x.ForCollection(sandwiches))
    .WithTable("Sandwiches")
    .AddAllColumns()
    .BulkInsert();

await bulkOperations.CommitTransactionAsync();

Console.WriteLine($" Done!");

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

IEnumerable<Sandwich> MakeSandwiches(IEnumerable<List<Filling>> fillingCombinations, IEnumerable<List<Condiment>> condimentCombinations)
{
    foreach(List<Filling> fillingCombination in fillingCombinations)
    {
        foreach (List<Condiment> condimentCombination in condimentCombinations)
        {
            Dictionary<Tastes, int> tastes = TasteValueHolder.ToDictionary(taste => taste.Key, taste => taste.Value);
            Dictionary<Powers, int> powers = PowerValueHolder.ToDictionary(power => power.Key, power => power.Value);
            Dictionary<Types, int> types = TypesValueHolder.ToDictionary(type => type.Key, type => type.Value);

            foreach (Filling filling in fillingCombination)
            {
                foreach (TasteValue tasteValue in filling.TasteValues)
                {
                    tastes[tasteValue.Name] += tasteValue.Value;
                }
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

            yield return new()
            {
                Fillings = string.Join(", ", fillingCombination.Where(filling => filling.Name != Fillings.None).Select(filling => filling.Name)),
                Condiments = string.Join(", ", condimentCombination.Where(condiment => condiment.Name != Condiments.None).Select(condiment => condiment.Name)),
                Cost = fillingCombination.Sum(filling => filling.Cost) + condimentCombination.Sum(condiment => condiment.Cost),
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