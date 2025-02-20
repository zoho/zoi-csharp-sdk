﻿using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Com.Zoho.API.Authenticator;
using Com.Zoho.API.Authenticator.Store;
using Com.Zoho.Officeintegrator.Logger;
using Com.Zoho.Officeintegrator.Exception;
using Com.Zoho.Officeintegrator.Util;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static Com.Zoho.Officeintegrator.Logger.Logger;

namespace Com.Zoho.Officeintegrator
{
    /// <summary>
    /// This class to initialize Zoho SDK.
    /// </summary>
    public class Initializer : IDisposable
    {
        private static  ThreadLocal<Initializer> LOCAL = new ThreadLocal<Initializer>();
        private static Initializer initializer;
        private Dc.Environment environment;
        private ITokenStore store;
        private List<IToken> tokens;
        public static JObject jsonDetails;
        private RequestProxy requestProxy;
        private SDKConfig sdkConfig;

        private static void Initialize(Dc.Environment environment, List<IToken> tokens, ITokenStore store, SDKConfig sdkConfig, Logger.Logger logger, RequestProxy proxy)
        {
            try
            {
                SDKLogger.Initialize(logger);
                try
                {
                    if(jsonDetails == null || jsonDetails.Count == 0)
                    {
                        string result = "";
                        Assembly assembly = Assembly.GetExecutingAssembly();
                        using (Stream stream = assembly.GetManifestResourceStream(assembly.GetName().Name + Constants.JSON_DETAILS_FILE_PATH))
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                result = reader.ReadToEnd();
                            }
                        }
                        jsonDetails = JObject.Parse(result);
                    }
                }
                catch (System.Exception e)
                {
                    throw new SDKException(Constants.JSON_DETAILS_ERROR, e);
                }
                initializer = new Initializer();
                initializer.environment = environment;
                initializer.sdkConfig = sdkConfig;
                initializer.requestProxy = proxy;
                initializer.store = store;
                initializer.tokens = tokens;
                SDKLogger.LogInfo(Constants.INITIALIZATION_SUCCESSFUL + initializer.ToString());
            }
            catch(SDKException e)
            {
                throw e;
            }
            catch (System.Exception e)
            {
                throw new SDKException(Constants.INITIALIZATION_EXCEPTION, e);
            }
        }

        private static void SwitchUser(Dc.Environment environment, List<IToken> tokens, SDKConfig sdkConfig, RequestProxy proxy)
        {
            Initializer initializer = new Initializer();
            initializer.environment = environment;
            initializer.store = Initializer.initializer.store;
            initializer.sdkConfig = sdkConfig;
            initializer.requestProxy = proxy;
            initializer.tokens = tokens;
            LOCAL.Value = initializer;
            SDKLogger.LogInfo(Constants.INITIALIZATION_SWITCHED + initializer.ToString());
        }

        /// <summary>
        /// This method to get record field information details.
        /// </summary>
        /// <param name="filePath">A String containing the class information details file path.</param>
        /// <returns></returns>
        public static JObject GetJSON(string filePath)
        {
            StreamReader sr = new StreamReader(filePath);
            string fileContent = sr.ReadToEnd();
            sr.Close();
            return JObject.Parse(fileContent);
        }

        /// <summary>
        /// This method to get Initializer class instance.
        /// </summary>
        /// <returns>A Initializer class instance representing the SDK configuration details.</returns>
        public static Initializer GetInitializer()
        {
            if (Initializer.LOCAL.Value != null)
            {
                return Initializer.LOCAL.Value;
            }
            return initializer;
        }

        /// <summary>
        /// This is a getter method to get API environment.
        /// </summary>
        /// <returns>A Environment representing the API environment.</returns>
        public Dc.Environment Environment
        {
            get
            {
                return environment;
            }
        }

        /// <summary>
        /// This is a getter method to get API environment.
        /// </summary>
        /// <returns>A TokenStore class instance containing the token store information.</returns>
        public ITokenStore Store
        {
            get
            {
                return store;
            }
        }

        /// <summary>
        /// This is a getter method to get OAuth client application information.
        /// </summary>
        /// <returns>A Token class instance representing the OAuth client application information.</returns>
        public List<IToken> Tokens
        {
            get
            {
                return tokens;
            }
        }
        
        /// <summary>
        /// This is a getter method to get Proxy information.
        /// </summary>
        /// <returns>A RequestProxy class instance representing the API Proxy information.</returns>
        public RequestProxy RequestProxy
        {
            get
            {
                return requestProxy;
            }
        }
        
        /// <summary>
        /// This is a getter method to get the SDK Configuration
        /// </summary>
        /// <returns>A SDKConfig instance representing the configuration</returns>
        public SDKConfig SDKConfig
        {
            get
            {
                return sdkConfig;
            }
        }

        public override string ToString()
        {
            return new StringBuilder().Append(Constants.IN_ENVIRONMENT).Append(GetInitializer().Environment.GetUrl()).Append(".").ToString();
        }

        public class Builder
        {
            private Dc.Environment environment;
            private ITokenStore store;
            private List<IToken> tokens;
            private RequestProxy requestProxy;
            private SDKConfig sdkConfig;
            private Logger.Logger logger;
            private string errorMessage = (Initializer.initializer != null) ? Constants.SWITCH_USER_ERROR : Constants.INITIALIZATION_ERROR;

            public Builder()
            {
                if(Initializer.GetInitializer() != null)
                {
                    Initializer previousInitializer = Initializer.GetInitializer();
                    environment = previousInitializer.Environment;
                    tokens = previousInitializer.Tokens;
                    sdkConfig = previousInitializer.SDKConfig;
                }
            }

            public void Initialize()
            {
                Utility.AssertNotNull(environment, errorMessage, Constants.ENVIRONMENT_ERROR_MESSAGE);
                if (store == null)
                {
                    bool isCreate = false;
                    foreach(IToken tokenInstance in this.tokens)
                    {
                        if(tokenInstance is OAuth2)
                        {
                            isCreate = true;
                            break;
                        }
                    }
                    if(isCreate)
                    {
                        store = new FileStore(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) + Path.DirectorySeparatorChar + Constants.TOKEN_FILE);
                    }
                }
                if (sdkConfig == null)
                {
                    sdkConfig = new SDKConfig.Builder().Build();
                }
                if (logger == null)
                {
                    logger = new Logger.Logger.Builder().Level(Levels.OFF).FilePath(null).Build();
                }
                Initializer.Initialize(this.environment, this.tokens, this.store, this.sdkConfig, this.logger, this.requestProxy);
            }

            public void SwitchUser()
            {
                Utility.AssertNotNull(Initializer.initializer, Constants.SDK_UNINITIALIZATION_ERROR, Constants.SDK_UNINITIALIZATION_MESSAGE);
                if (this.sdkConfig == null)
                {
                    this.sdkConfig = new SDKConfig.Builder().Build();
                }
                Initializer.SwitchUser(this.environment, this.tokens, this.sdkConfig, this.requestProxy);
            }

            public Builder Logger(Logger.Logger logger)
            {
                this.logger = logger;
                return this;
            }

            public Builder Tokens(List<IToken> tokens)
            {
                Utility.AssertNotNull(tokens, errorMessage, Constants.TOKEN_ERROR_MESSAGE);
                this.tokens = tokens;
                return this;
            }

            public Builder SDKConfig(SDKConfig sdkConfig)
            {
			    this.sdkConfig = sdkConfig;
			    return this;
		    }

            public Builder RequestProxy(RequestProxy requestProxy)
            {
                this.requestProxy = requestProxy;
                return this;
            }

            public Builder Store(ITokenStore store)
            {
			    this.store = store;
			    return this;
		    }

            public Builder Environment(Dc.Environment environment)
            {
                Utility.AssertNotNull(environment, errorMessage, Constants.ENVIRONMENT_ERROR_MESSAGE);
			    this.environment = environment;
			    return this;
            }
        }

        public void Dispose()
        {
            ((IDisposable)LOCAL).Dispose();
        }
    }
}
