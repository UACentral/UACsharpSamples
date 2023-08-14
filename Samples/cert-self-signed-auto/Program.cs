using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using static System.Net.Mime.MediaTypeNames;

public static class Program
{
    public static async Task Main(string[] args)
    {
        /*
         * OPC Foundation .Net Standard gotcha
         * Creating self signed certificate usng the factory certificate generator replaces "localhost" with machine name
         */
        const string appName = "OPC UA minimal client self signed auto";
        const ApplicationType appType = ApplicationType.Client;

        CertificateIdentifier certificateIdentifier = new CertificateIdentifier();
        certificateIdentifier.StoreType = "Directory";
        certificateIdentifier.StorePath = "%LocalApplicationData%/Tuko/pki/own";
        certificateIdentifier.SubjectName = Utils.ReplaceDCLocalhost("CN=UA Client self signed auto, C=US, S=Texas, O=Tuko, DC=localhost");

        SecurityConfiguration securityConfiguration = new SecurityConfiguration();
        securityConfiguration.ApplicationCertificate = certificateIdentifier;

        var config = new ApplicationConfiguration()
        {
            ApplicationName = appName,
            ApplicationType = appType,
            ApplicationUri = Utils.ReplaceLocalhost("urn:localhost:UA:Samples:Self:Sgined:Auto"),
            ClientConfiguration = new ClientConfiguration()
            {
                DefaultSessionTimeout = 60_000,
            },
            SecurityConfiguration = securityConfiguration,
        };

        CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider(null);
        ApplicationInstance appInst = new ApplicationInstance
        {
            ApplicationName = appName,
            ApplicationType = appType,
            CertificatePasswordProvider = PasswordProvider,
        };
        appInst.ApplicationConfiguration = config;

        // This call will check if application certificate exists, if not it creates self signed certificate.
        // OPC Foundation's code has lots of side effects and names are not obvious
        // this will update appInst.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate
        bool haveAppCertificate = await appInst.CheckApplicationInstanceCertificate(false, minimumKeySize: 0).ConfigureAwait(false);
        if (!haveAppCertificate)
        {
            throw new Exception("Application instance certificate invalid!");
        }


        Uri serverUrl = new Uri("opc.tcp://localhost:62541/Quickstarts/ReferenceServer");
        var endpointDescription = CoreClientUtils.SelectEndpoint(config, serverUrl.ToString(), false);

        EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(config);
        ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

        var session = await Opc.Ua.Client.Session.Create(
            config,
            null,
            endpoint,
            true,
            false,
            config.ApplicationName,
            (uint)config.ClientConfiguration.DefaultSessionTimeout,
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