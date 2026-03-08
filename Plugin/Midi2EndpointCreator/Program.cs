using Microsoft.Windows.Devices.Midi2;
using Microsoft.Windows.Devices.Midi2.Endpoints.BasicLoopback;
using Microsoft.Windows.Devices.Midi2.Initialization;
using Windows.Storage.Search;

// Usage: Midi2EndpointCreator.exe <command> [args...]
// Commands:
//   create <name1> [name2] ...  - Creates loopback endpoint pairs
//   remove <associationId>      - Removes a loopback endpoint pair
//
// Output on success (one line per pair):
//   OK|<endpointName>|<associationIdA>|<endpointDeviceIdA>|<associationIdA>|<endpointDeviceIdB>
// Output on failure:
//   ERROR|<endpointName>|<message>

// args = args.Append("remove").Append("murks").ToArray();

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Midi2EndpointCreator.exe create <endpointName1> [endpointName2] ...");
    Console.Error.WriteLine("       Midi2EndpointCreator.exe remove <associationId>");
    return 1;
}

using var initializer = MidiDesktopAppSdkInitializer.Create();

if (initializer == null || !initializer.InitializeSdkRuntime())
{
    Console.Error.WriteLine("ERROR|*|Failed to initialize SDK runtime");
    return 1;
}

if (!initializer.EnsureServiceAvailable())
{
    Console.Error.WriteLine("ERROR|*|Windows MIDI Services not available");
    return 1;
}

var command = args[0].ToLowerInvariant();

switch (command)
{
    case "create":
        return CreateEndpoints(args.Skip(1));

    case "remove":
        return RemoveEndpoints(args[1]);

    default:
        Console.Error.WriteLine($"ERROR|*|Unknown command: {command}");
        return 1;
}

static int CreateEndpoints(IEnumerable<string> endpointNames)
{
    using var session = MidiSession.Create("Midi2EndpointCreator");
    if (session == null)
    {
        Console.Error.WriteLine("ERROR|*|Failed to create MIDI session");
        return 1;
    }

    // Enumerate all loopback endpoints visible to the system
    var allEndpoints = MidiEndpointDeviceInformation.FindAll(MidiEndpointDeviceInformationSortOrder.Name,
                                                             MidiEndpointDeviceInformationFilters.AllStandardEndpoints);

    var hasError = false;

    foreach (var endpointName in endpointNames)
    {
        var nameIn = $"{endpointName} In";
        var nameOut = $"{endpointName} Out";

        var existingIn = allEndpoints.FirstOrDefault(e => e.Name == nameIn);
        var existingOut = allEndpoints.FirstOrDefault(e => e.Name == nameOut);

        if (existingIn != null || existingOut != null)
        {
            Console.Error.WriteLine($"EXIST|{endpointName}|Endpoints with matching names already exists");
            continue;
        }

        var definitionA = new MidiBasicLoopbackEndpointDefinition();
        definitionA.Name = $"{endpointName} In";
        definitionA.UniqueId = Guid.NewGuid().ToString("N")[..32];

        var definitionB = new MidiBasicLoopbackEndpointDefinition();
        definitionB.Name = $"{endpointName} Out";
        definitionB.UniqueId = Guid.NewGuid().ToString("N")[..32];

        string endpointDeviceIdA = "";

        var associationIdA = Guid.NewGuid();
        var associationIdB = Guid.NewGuid();

        var configA = new MidiBasicLoopbackEndpointCreationConfig(associationIdA, definitionA);
        var result = MidiBasicLoopbackEndpointManager.CreateTransientLoopbackEndpoint(configA);

        if (result.Success)
        {
            endpointDeviceIdA = result.EndpointDeviceId;
            var configB = new MidiBasicLoopbackEndpointCreationConfig(associationIdB, definitionB);
            result = MidiBasicLoopbackEndpointManager.CreateTransientLoopbackEndpoint(configB);
        }

        if (result.Success)
        {
            Console.WriteLine($"OK|{endpointName}|{associationIdA}|{endpointDeviceIdA}|{associationIdB}|{result.EndpointDeviceId}");
        }
        else
        {
            Console.WriteLine($"ERROR|{endpointName}|Failed to create endpoint pair");
            hasError = true;
        }
    }

    return hasError ? 1 : 0;
}

static int RemoveEndpoints(string associationIdStr)
{
    if (!Guid.TryParse(associationIdStr, out var associationId))
    {
        Console.Error.WriteLine($"ERROR|{associationIdStr}|Invalid GUID");
        return 1;
    }

    if (MidiBasicLoopbackEndpointManager.RemoveTransientLoopbackEndpoint(new MidiBasicLoopbackEndpointRemovalConfig(associationId)))
    {
        Console.WriteLine($"OK|removed|{associationId}");
        return 0;
    }

    Console.Error.WriteLine($"ERROR|{associationId}|Failed to remove endpoints");
    return 1;
}
