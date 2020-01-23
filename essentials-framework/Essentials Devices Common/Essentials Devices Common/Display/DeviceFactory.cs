using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Devices.Displays
{
	public class DisplayDeviceFactory
	{
		public static IKeyed GetDevice(DeviceConfig dc)
		{
			var key = dc.Key;
			var name = dc.Name;
			var type = dc.Type;
			var properties = dc.Properties;

			var typeName = dc.Type.ToLower();

			try
			{
				if (typeName == "necmpsx")
				{
					var comm = CommFactory.CreateCommForDevice(dc);
					if (comm != null)
						return new NecPSXMDisplay(dc.Key, dc.Name, comm);
				}
				if (typeName == "panasonicthef")
				{
					var comm = CommFactory.CreateCommForDevice(dc);
					if (comm != null)
						return new PanasonicThefDisplay(dc.Key, dc.Name, comm);
				}
                else if(typeName == "samsungmdc")
                {
                    var directoryPrefix = string.Format("{0}Display{1}Schema{1}", Global.ApplicationDirectoryPrefix, Global.DirectorySeparator);

                    var schemaFilePath = directoryPrefix + "SamsungMDCPropertiesConfigSchema.json";
                    Debug.Console(0, Debug.ErrorLogLevel.Notice, "Loading Schema from path: {0}", schemaFilePath);

                    var jsonConfig = dc.Properties.ToString();

                    if (File.Exists(schemaFilePath))
                    {
                        // Attempt to validate config against schema
                        Global.ValidateSchema(jsonConfig, schemaFilePath);
                    }
                    else
                        Debug.Console(0, Debug.ErrorLogLevel.Warning, "No Schema found at path: {0}", schemaFilePath);


                    var comm = CommFactory.CreateCommForDevice(dc);
                    if (comm != null)
                        return new SamsungMDC(dc.Key, dc.Name, comm, dc.Properties["id"].Value<string>());
                }
                if (typeName == "avocorvtf")
                {
                    var comm = CommFactory.CreateCommForDevice(dc);
                    if (comm != null)
                        return new AvocorDisplay(dc.Key, dc.Name, comm, null);
                }
                   
			}
			catch (Exception e)
			{
				Debug.Console(0, "Displays factory: Exception creating device type {0}, key {1}: \nCONFIG JSON: {2} \nERROR: {3}\n\n", 
                    dc.Type, dc.Key, JsonConvert.SerializeObject(dc), e);
				return null;
			}

			return null;
		}
	}
}