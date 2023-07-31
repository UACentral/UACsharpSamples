using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "OPC UA minimal client Read Sync",
            ApplicationType = ApplicationType.Client,
        };
        var clientConfig = new ClientConfiguration
        {
            DefaultSessionTimeout = 60000,
        };
        config.ClientConfiguration = clientConfig;

        ApplicationInstance appInst = new ApplicationInstance
        {
            ApplicationName = "OPC UA minimal client application instance",
            ApplicationType = ApplicationType.Client,
        };
        appInst.ApplicationConfiguration = config;

        Uri serverUrl = new Uri("opc.tcp://localhost:62541/Quickstarts/ReferenceServer");
        var endpointDescription = CoreClientUtils.SelectEndpoint(appInst.ApplicationConfiguration, serverUrl.ToString(), false);

        EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(appInst.ApplicationConfiguration);
        ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

        uint SessionLifeTime = 60 * 1000;
        var session = await Opc.Ua.Client.Session.Create(
            appInst.ApplicationConfiguration,
            null,
            endpoint,
            true,
            false,
            appInst.ApplicationConfiguration.ApplicationName,
            SessionLifeTime,
            new UserIdentity(),
            null
            ).ConfigureAwait(false);

        // build a list of nodes to be read
        ReadValueIdCollection nodesToRead = new ReadValueIdCollection()
                {
                    // Value of ServerStatus
                    new ReadValueId() { NodeId = Variables.Server_ServerStatus, AttributeId = Attributes.Value },
                    // BrowseName of ServerStatus_StartTime
                    new ReadValueId() { NodeId = Variables.Server_ServerStatus_StartTime, AttributeId = Attributes.BrowseName },
                    // Value of ServerStatus_StartTime
                    new ReadValueId() { NodeId = Variables.Server_ServerStatus_StartTime, AttributeId = Attributes.Value }
                };

        session.Read(
            null,
            0,
            TimestampsToReturn.Both,
            nodesToRead,
            out DataValueCollection resultsValues,
            out DiagnosticInfoCollection diagnosticInfos);

        ClientBase.ValidateResponse(resultsValues, nodesToRead);
        foreach (DataValue result in resultsValues)
        {
            Console.WriteLine("Read Value = {0} , StatusCode = {1}", result.Value, result.StatusCode);
        }
        session.Close();

        Console.WriteLine("End OPC UA client");
    }
}