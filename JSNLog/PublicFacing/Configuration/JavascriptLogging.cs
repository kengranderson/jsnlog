﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using JSNLog.Exceptions;
using JSNLog.Infrastructure;
using System.Text.RegularExpressions;

namespace JSNLog
{
    public class JavascriptLogging
    {
        /// <summary>
        /// Call this method for every request to generate a script tag with JavaScript
        /// that configures all loggers and appenders, based on the jsnlog element in the web.config.
        /// </summary>
        /// <param name="requestId">
        /// Request Id to be included in all logging requests sent by jsnlog.js from the client.
        /// If null, a new request id will be generated (the same one that will be returned from RequestId method).
        /// </param>
        /// <returns></returns>
        public static string Configure(string requestId = null)
        {
            StringBuilder sb = new StringBuilder();

            var configProcessor = new ConfigProcessor();
            configProcessor.ProcessRoot(requestId, sb);

            return sb.ToString();
        }

        /// <summary>
        /// Returns a request id that is unique to this request.
        /// 
        /// However, if the request is a log request from jsnlog.js, than this method returns the requestId travelling 
        /// in the request.
        /// 
        /// The site can call this method to get the request id for use in server side logging.
        /// </summary>
        /// <returns></returns>
        public static string RequestId()
        {
            string requestId = JSNLog.Infrastructure.RequestId.GetFromRequest();

            // If requestId is empty string, regard that as a valid requestId.
            // jsnlog.js will send such request ids when the request id has not been
            // set. In that case, you don't want to generate a new request id for
            // a log request, because that would be confusing.
            if (requestId == null)
            {
                requestId = JSNLog.Infrastructure.RequestId.Get();
            }

            return requestId;
        }

        // Definitions for the OnLogging event. Search for OnLogging to see how it is used.
        public static event LoggingHandler OnLogging;

        internal static void RaiseLoggingEvent(LoggingEventArgs loggingEventArgs)
        {
            if (OnLogging != null)
            {
                OnLogging(loggingEventArgs);
            }
        }

        #region JsnlogConfiguration

        private static JsnlogConfiguration _jsnlogConfiguration = null;

        // Seam used for unit testing. During unit testing, gets an xml element created by the test. 
        // During production get the jsnlog element from web.config.
        //
        // >>>>>>
        // Note that calling this method with a given xe is a way to cache that xe's config
        // for the next call to GetJsnlogConfiguration().
        internal static JsnlogConfiguration GetJsnlogConfiguration(Func<XmlElement> lxe)
        {
            if (_jsnlogConfiguration == null)
            {
                XmlElement xe = lxe();
                if (xe != null)
                {
                    _jsnlogConfiguration = XmlHelpers.DeserialiseXml<JsnlogConfiguration>(xe);
                }
            }

            // If there is no configuration, return the default configuration
            return _jsnlogConfiguration ?? new JsnlogConfiguration();
        }

        public static JsnlogConfiguration GetJsnlogConfiguration()
        {
            return GetJsnlogConfiguration(() => XmlHelpers.RootElement());
        }

        internal static void SetJsnlogConfiguration(Func<XmlElement> lxe, JsnlogConfiguration jsnlogConfiguration)
        {
            // Always allow setting the config to null, because GetJsnlogConfiguration retrieves web.config when config is null.
            if (jsnlogConfiguration != null)
            {
                XmlElement xe = lxe();
                if (xe != null)
                {
                    throw new ConflictingConfigException();
                }
            }

            _jsnlogConfiguration = jsnlogConfiguration;
        }

        public static void SetJsnlogConfiguration(JsnlogConfiguration jsnlogConfiguration)
        {
            SetJsnlogConfiguration(() => XmlHelpers.RootElement(), jsnlogConfiguration);
        }

        #endregion
    }
}