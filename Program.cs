using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using RailwayOrienteProgramming;

internal class Program
{
    // IPropagatorBlock
    // Delegates -> Action / Func / Predicate
    // TransformBlock

    // ROP STEPS
    // 1. define data contract - make a class - NameData
    // 2. define method to return propagator block
    // (TransformBlock, ActionBlock, BroadcastBlock)
    // 3. implement method to execute pipeline

    private static bool shouldPrintName = true;

    private static async Task Main(string[] args)
    {
        // PrintName();
        await GetNameData();
    }

    public static async Task<NameData> GetNameData()
    {

        NameData nameData = new();
        NameData returnData = new();

        var namePipeline = CreateNamePipeline();

        await namePipeline.SendAsync(nameData);

        namePipeline.Complete();

        while (await namePipeline.OutputAvailableAsync())
        {
            returnData = await namePipeline.ReceiveAsync();
        }

        await namePipeline.Completion;

        return returnData;

    }

    public static IPropagatorBlock<NameData, NameData> CreateNamePipeline()
    {
        var firstNameBlock = GetFirstNameBlock();
        var lastNameBlock = GetLastNameBlock();
        var fullNameBlock = GetFullNameBlock();

        firstNameBlock.LinkTo(lastNameBlock, new DataflowLinkOptions { PropagateCompletion = true });
        RegisterErrorHandlingCallback(firstNameBlock, lastNameBlock);

        lastNameBlock.LinkTo(fullNameBlock, new DataflowLinkOptions { PropagateCompletion = true });
        RegisterErrorHandlingCallback(lastNameBlock, fullNameBlock);

        return DataflowBlock.Encapsulate(firstNameBlock, fullNameBlock);
    }

    private static TransformBlock<NameData, NameData> GetFirstNameBlock()
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMinutes(1));

        return new(data =>
        {
            data.FirstName = "Bart";
            Console.WriteLine($"firstNameBlock firstName: {data.FirstName}");
            Console.WriteLine($"firstNameBlock lastName: {data.LastName}");
            Console.WriteLine($"firstNameBlock fullName: {data.FullName}");
            // throw new Exception("first name block throws an exception");
            return data;
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cts.Token });
    }

    private static TransformBlock<NameData, NameData> GetFullNameBlock()
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        return new(data =>
        {
            data.FullName = $"{data.FirstName} {data.LastName}";
            Console.WriteLine($"fullNameBlock firstName: {data.FullName}");
            Console.WriteLine($"fullNameBlock lastName: {data.LastName}");
            Console.WriteLine($"fullNameBlock fullName: {data.FullName}");

            return data;
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cts.Token });
    }

    private static TransformBlock<NameData, NameData> GetLastNameBlock()
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        return new(data =>
        {
            data.LastName = "Van Hoey";
            Console.WriteLine($"lastNameBlock firstName: {data.FirstName}");
            Console.WriteLine($"lastNameBlock lastName: {data.LastName}");
            Console.WriteLine($"lastNameBlock fullName: {data.FullName}");
            return data;
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cts.Token });
    }

    public static void RegisterErrorHandlingCallback(IDataflowBlock target, IDataflowBlock source)
    {
        target.Completion.ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Console.WriteLine("target block error occurred");
            }
            else if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine("target block completed successfully");
            }
        });
    }




    public static void PrintName()
    {
        if (shouldPrintName)
        {
            try
            {
                var firstName = ReturnFirstName();
                var lastName = ReturnLastName();
                var fullName = ReturnFullName(firstName, lastName);
                Console.WriteLine($"FullName: {fullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }



    public static string ReturnFirstName()
    {
        Console.WriteLine("FirstName: Bart");
        return "Bart";
    }

    public static string ReturnLastName()
    {
        Console.WriteLine("LastName: Van Hoey");
        return "Van Hoey";
    }

    public static string ReturnFullName(string firstName, string lastName)
    {
        return firstName + " " + lastName;
    }
}