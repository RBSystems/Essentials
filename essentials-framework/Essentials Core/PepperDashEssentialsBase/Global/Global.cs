using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;

using PepperDash.Core;
using PepperDash.Essentials.License;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;



namespace PepperDash.Essentials.Core
{
    /// <summary>
    /// Global application properties
    /// </summary>
	public static class Global
	{
        /// <summary>
        /// The control system the application is running on
        /// </summary>
		public static CrestronControlSystem ControlSystem { get; set; }

		public static LicenseManager LicenseManager { get; set; }

        /// <summary>
        /// The file path prefix to the folder containing configuration files
        /// </summary>
        public static string FilePathPrefix { get; private set; }

        /// <summary>
        /// Returns the directory separator character based on the running OS
        /// </summary>
        public static char DirectorySeparator
        {
            get
            {
                return System.IO.Path.DirectorySeparatorChar;
            }
        }

        /// <summary>
        /// The file path prefix to the folder containing the application files (including embedded resources)
        /// </summary>
        public static string ApplicationDirectoryPrefix 
        {
            get
            {
                string fmt = "00.##";
                var appNumber = InitialParametersClass.ApplicationNumber.ToString(fmt);
                return  string.Format("{0}{1}Simpl{1}app{2}{1}", Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory(), Global.DirectorySeparator,appNumber );
            }
        }

        /// <summary>
        /// Wildcarded config file name for global reference
        /// </summary>
        public const string ConfigFileName = "*configurationFile*.json";

        /// <summary>
        /// Sets the file path prefix
        /// </summary>
        /// <param name="prefix"></param>
        public static void SetFilePathPrefix(string prefix)
        {
            FilePathPrefix = prefix;
        }

        /// <summary>
        /// Attempts to validate the JSON against the specified schema
        /// </summary>
        /// <param name="json">JSON to be validated</param>
        /// <param name="schemaFileName">File name of schema to validate against</param>
        public static void ValidateSchema(string json, string schemaFileName)
        {
            Debug.Console(0, Debug.ErrorLogLevel.Notice, "Validating Config File against Schema...");
            JObject config = JObject.Parse(json);

            using (StreamReader fileStream = new StreamReader(schemaFileName))
            {
                JsonSchema schema = JsonSchema.Parse(fileStream.ReadToEnd());

                if (config.IsValid(schema))
                    Debug.Console(0, Debug.ErrorLogLevel.Notice, "Configuration successfully Validated Against Schema");
                else
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Warning, "Validation Errors Found in Configuration:");
                    config.Validate(schema, Json_ValidationEventHandler);
                }
            }
        }

        /// <summary>
        /// Event Handler callback for JSON validation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static void Json_ValidationEventHandler(object sender, ValidationEventArgs args)
        {
            Debug.Console(0, Debug.ErrorLogLevel.Error, "JSON Validation error at line {0} position {1}: {2}", args.Exception.LineNumber, args.Exception.LinePosition, args.Message);
        }

		static Global()
		{
			// Fire up CrestronDataStoreStatic
			var err = CrestronDataStoreStatic.InitCrestronDataStore();
			if (err != CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
			{
				CrestronConsole.PrintLine("Error starting CrestronDataStoreStatic: {0}", err);
				return;
			}
		}

	}
}