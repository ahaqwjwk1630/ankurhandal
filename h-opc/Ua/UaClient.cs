using Hylasoft.Opc.Common;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hylasoft.Opc.Ua
{
    /// <summary>
    /// Client Implementation for UA
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling",
      Justification = "Doesn't make sense to split this class")]
    public class UaClient : IOpcClient
    {
        #region # Events, Fields and Constructors

        /// <summary>
        /// Default monitor interval in Milliseconds
        /// </summary>
        private const int DefaultMonitorInterval = 100;

        /// <summary>
        /// This event is raised when the connection to the OPC server is lost.
        /// </summary>
        public event EventHandler ServerConnectionLost;

        /// <summary>
        /// This event is raised when the connection to the OPC server is restored.
        /// </summary>
        public event EventHandler ServerConnectionRestored;

        /// <summary>
        /// Options to configure the UA client session
        /// </summary>
        private readonly UaClientOptions _options = new UaClientOptions();

        /// <summary>
        /// OPC Server URL
        /// </summary>
        private readonly Uri _serverUrl;

        /// <summary>
        /// OPC Foundation underlying session object
        /// </summary>
        private Session _session;

        /// <summary>
        /// Creates a server object
        /// </summary>
        /// <param name="serverUrl">the url of the server to connect to</param>
        public UaClient(Uri serverUrl)
        {
            this._serverUrl = serverUrl;
            this.Status = OpcStatus.NotConnected;
        }

        /// <summary>
        /// Creates a server object
        /// </summary>
        /// <param name="serverUrl">the url of the server to connect to</param>
        /// <param name="options">custom options to use with ua client</param>
        public UaClient(Uri serverUrl, UaClientOptions options)
        {
            this._serverUrl = serverUrl;
            this._options = options;
            this.Status = OpcStatus.NotConnected;
        }

        #endregion

        #region # Properties

        #region Monitor interval in Milliseconds —— int? MonitorInterval
        /// <summary>
        /// Monitor interval in Milliseconds
        /// </summary>
        public int? MonitorInterval { get; set; }
        #endregion

        #region Gets the current status of the OPC Client —— OpcStatus Status
        /// <summary>
        /// Gets the current status of the OPC Client
        /// </summary>
        public OpcStatus Status { get; private set; }
        #endregion

        #region OPC Foundation underlying session object —— Session Session
        /// <summary>
        /// OPC Foundation underlying session object
        /// </summary>
        public Session Session
        {
            get { return this._session; }
        }
        #endregion

        #region Options to configure the UA client session —— UaClientOptions Options
        /// <summary>
        /// Options to configure the UA client session
        /// </summary>
        public UaClientOptions Options
        {
            get { return this._options; }
        }
        #endregion

        #endregion

        #region # Methods

        //Implements

        #region Connect the client to the OPC Server —— void Connect()
        /// <summary>
        /// Connect the client to the OPC Server
        /// </summary>
        public void Connect()
        {
            if (this.Status == OpcStatus.Connected)
            {
                return;
            }

            this._session = this.InitializeSession(this._serverUrl);
            this._session.KeepAlive += this.OnSessionKeepAlive;
            this._session.SessionClosing += this.OnSessionClosing;

            this.Status = OpcStatus.Connected;
        }
        #endregion

        #region Gets the datatype of an OPC tag —— Type GetDataType(string tag)
        /// <summary>
        /// Gets the datatype of an OPC tag
        /// </summary>
        /// <param name="tag">Tag to get datatype of</param>
        /// <returns>System Type</returns>
        public Type GetDataType(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);
            DataValueCollection results;
            DiagnosticInfoCollection diag;
            this._session.Read(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                results: out results,
                diagnosticInfos: out diag);
            BuiltInType type = results[0].WrappedValue.TypeInfo.BuiltInType;
            return Type.GetType("System." + type.ToString());
        }
        #endregion

        #region Read a tag —— ReadEvent Read(string tag)
        /// <summary>
        /// Read a tag
        /// </summary>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        public ReadEvent Read(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);
            this._session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out DataValueCollection results, out DiagnosticInfoCollection diag);
            DataValue val = results[0];

            ReadEvent readEvent = new ReadEvent
            {
                Value = val.Value,
                SourceTimestamp = val.SourceTimestamp,
                ServerTimestamp = val.ServerTimestamp
            };

            if (StatusCode.IsGood(val.StatusCode)) readEvent.Quality = Quality.Good;
            if (StatusCode.IsBad(val.StatusCode)) readEvent.Quality = Quality.Bad;

            return readEvent;
        }
        #endregion

        #region Read a tag —— ReadEvent<T> Read<T>(string tag)
        /// <summary>
        /// Read a tag
        /// </summary>
        /// <typeparam name="T">The type of tag to read</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        public ReadEvent<T> Read<T>(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);
            this._session.Read(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                results: out DataValueCollection results,
                diagnosticInfos: out DiagnosticInfoCollection diag);
            DataValue val = results[0];

            ReadEvent<T> readEvent = new ReadEvent<T>
            {
                Value = (T)val.Value,
                SourceTimestamp = val.SourceTimestamp,
                ServerTimestamp = val.ServerTimestamp
            };
            if (StatusCode.IsGood(val.StatusCode)) readEvent.Quality = Quality.Good;
            if (StatusCode.IsBad(val.StatusCode)) readEvent.Quality = Quality.Bad;

            return readEvent;
        }
        #endregion

        #region Read a tag asynchronously —— Task<ReadEvent<T>> ReadAsync<T>(string tag)
        /// <summary>
        /// Read a tag asynchronously
        /// </summary>
        /// <typeparam name="T">The type of tag to read</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        public Task<ReadEvent<T>> ReadAsync<T>(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);

            // Wrap the ReadAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
            TaskCompletionSource<ReadEvent<T>> taskCompletionSource = new TaskCompletionSource<ReadEvent<T>>();
            this._session.BeginRead(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                callback: ar =>
                {
                    ResponseHeader response = this._session.EndRead(
                      result: ar,
                      results: out DataValueCollection results,
                      diagnosticInfos: out DiagnosticInfoCollection diag);

                    try
                    {
                        this.CheckReturnValue(response.ServiceResult);
                        DataValue val = results[0];

                        ReadEvent<T> readEvent = new ReadEvent<T>
                        {
                            Value = (T)val.Value,
                            SourceTimestamp = val.SourceTimestamp,
                            ServerTimestamp = val.ServerTimestamp
                        };
                        if (StatusCode.IsGood(val.StatusCode)) readEvent.Quality = Quality.Good;
                        if (StatusCode.IsBad(val.StatusCode)) readEvent.Quality = Quality.Bad;

                        taskCompletionSource.TrySetResult(readEvent);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                },
                asyncState: null);

            return taskCompletionSource.Task;
        }
        #endregion

        #region Write a value on the specified opc tag —— void Write<T>(string tag, T item)
        /// <summary>
        /// Write a value on the specified opc tag
        /// </summary>
        /// <typeparam name="T">The type of tag to write on</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` writes on the tag `bar` on the folder `foo`</param>
        /// <param name="item">The value for the item to write</param>
        public void Write<T>(string tag, T item)
        {
            WriteValueCollection nodesToWrite = this.BuildWriteValueCollection(tag, Attributes.Value, item);
            this._session.Write(null, nodesToWrite, out StatusCodeCollection results, out DiagnosticInfoCollection diag);

            this.CheckReturnValue(results[0]);
        }
        #endregion

        #region Write a value on the specified opc tag asynchronously —— Task WriteAsync<T>(string tag, T item)
        /// <summary>
        /// Write a value on the specified opc tag asynchronously
        /// </summary>
        /// <typeparam name="T">The type of tag to write on</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` writes on the tag `bar` on the folder `foo`</param>
        /// <param name="item">The value for the item to write</param>
        public Task WriteAsync<T>(string tag, T item)
        {
            WriteValueCollection nodesToWrite = this.BuildWriteValueCollection(tag, Attributes.Value, item);

            // Wrap the WriteAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
            TaskCompletionSource<StatusCode> taskCompletionSource = new TaskCompletionSource<StatusCode>();
            this._session.BeginWrite(
                requestHeader: null,
                nodesToWrite: nodesToWrite,
                callback: ar =>
                {
                    ResponseHeader response = this._session.EndWrite(
                      result: ar,
                      results: out StatusCodeCollection results,
                      diagnosticInfos: out DiagnosticInfoCollection diag);
                    try
                    {
                        this.CheckReturnValue(response.ServiceResult);
                        this.CheckReturnValue(results[0]);
                        taskCompletionSource.SetResult(response.ServiceResult);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                },
                asyncState: null);

            return taskCompletionSource.Task;
        }
        #endregion

        #region Monitor the specified tag for changes —— void Monitor(string tag, Action<ReadEvent, Action> callback)
        /// <summary>
        /// Monitor the specified tag for changes
        /// </summary>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is a MonitorEvent object which represents the data point, the second is an `unsubscribe` function to unsubscribe the callback</param>
        public void Monitor(string tag, Action<ReadEvent, Action> callback)
        {
            //Updated by Lee
            NodeId nodeId = new NodeId(tag);

            Subscription sub = new Subscription
            {
                PublishingInterval = this.MonitorInterval ?? DefaultMonitorInterval,
                PublishingEnabled = true,
                LifetimeCount = this._options.SubscriptionLifetimeCount,
                KeepAliveCount = this._options.SubscriptionKeepAliveCount,
                DisplayName = tag,
                Priority = byte.MaxValue
            };

            MonitoredItem item = new MonitoredItem
            {
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                DisplayName = tag,
                SamplingInterval = this.MonitorInterval ?? DefaultMonitorInterval,
            };
            sub.AddItem(item);
            this._session.AddSubscription(sub);
            sub.Create();
            sub.ApplyChanges();

            item.Notification += (monitoredItem, args) =>
            {
                MonitoredItemNotification p = (MonitoredItemNotification)args.NotificationValue;
                object value = p.Value.WrappedValue.Value;
                Action unsubscribe = () =>
                {
                    sub.RemoveItems(sub.MonitoredItems);
                    sub.Delete(true);
                    this._session.RemoveSubscription(sub);
                    sub.Dispose();
                };

                ReadEvent monitorEvent = new ReadEvent
                {
                    Value = value,
                    SourceTimestamp = p.Value.SourceTimestamp,
                    ServerTimestamp = p.Value.ServerTimestamp
                };
                if (StatusCode.IsGood(p.Value.StatusCode)) monitorEvent.Quality = Quality.Good;
                if (StatusCode.IsBad(p.Value.StatusCode)) monitorEvent.Quality = Quality.Bad;

                callback(monitorEvent, unsubscribe);
            };
        }
        #endregion

        #region Monitor the specified tag for changes —— void Monitor<T>(string tag, Action<ReadEvent<T>, Action> callback)
        /// <summary>
        /// Monitor the specified tag for changes
        /// </summary>
        /// <typeparam name="T">the type of tag to monitor</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is a MonitorEvent object which represents the data point, the second is an `unsubscribe` function to unsubscribe the callback</param>
        public void Monitor<T>(string tag, Action<ReadEvent<T>, Action> callback)
        {
            //Updated by Lee
            NodeId nodeId = new NodeId(tag);

            Subscription sub = new Subscription
            {
                PublishingInterval = this.MonitorInterval ?? DefaultMonitorInterval,
                PublishingEnabled = true,
                LifetimeCount = this._options.SubscriptionLifetimeCount,
                KeepAliveCount = this._options.SubscriptionKeepAliveCount,
                DisplayName = tag,
                Priority = byte.MaxValue
            };

            MonitoredItem item = new MonitoredItem
            {
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                DisplayName = tag,
                SamplingInterval = this.MonitorInterval ?? DefaultMonitorInterval
            };
            sub.AddItem(item);
            this._session.AddSubscription(sub);
            sub.Create();
            sub.ApplyChanges();

            item.Notification += (monitoredItem, args) =>
            {
                MonitoredItemNotification p = (MonitoredItemNotification)args.NotificationValue;
                object t = p.Value.WrappedValue.Value;
                Action unsubscribe = () =>
                {
                    sub.RemoveItems(sub.MonitoredItems);
                    sub.Delete(true);
                    this._session.RemoveSubscription(sub);
                    sub.Dispose();
                };

                ReadEvent<T> monitorEvent = new ReadEvent<T>
                {
                    Value = (T)t,
                    SourceTimestamp = p.Value.SourceTimestamp,
                    ServerTimestamp = p.Value.ServerTimestamp
                };
                if (StatusCode.IsGood(p.Value.StatusCode)) monitorEvent.Quality = Quality.Good;
                if (StatusCode.IsBad(p.Value.StatusCode)) monitorEvent.Quality = Quality.Bad;

                callback(monitorEvent, unsubscribe);
            };
        }
        #endregion

        #region Monitor the specified tags for changes —— void Monitor(IEnumerable<string> tags...
        /// <summary>
        /// Monitor the specified tags for changes
        /// </summary>
        /// <param name="tags">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is the new values of the nodes, the second is an `unsubscribe` function to unsubscribe the callback</param>
        public void Monitor(IEnumerable<string> tags, Action<IDictionary<string, ReadEvent>, Action> callback)
        {
            //Updated by Lee

            #region # Check

            tags = tags?.Distinct().ToArray() ?? new string[0];
            if (!tags.Any())
            {
                throw new ArgumentNullException(nameof(tags), "tags cannot be empty !");
            }

            #endregion

            Subscription sub = new Subscription
            {
                PublishingInterval = this.MonitorInterval ?? DefaultMonitorInterval,
                PublishingEnabled = true,
                LifetimeCount = this._options.SubscriptionLifetimeCount,
                KeepAliveCount = this._options.SubscriptionKeepAliveCount,
                DisplayName = string.Join(",", tags),
                Priority = byte.MaxValue
            };
            Action unsubscribe = () =>
            {
                sub.RemoveItems(sub.MonitoredItems);
                sub.Delete(true);
                this._session.RemoveSubscription(sub);
                sub.Dispose();
            };

            IDictionary<string, ReadEvent> readEvents = tags.ToDictionary(x => x, x => new ReadEvent());
            foreach (string tag in tags)
            {
                NodeId nodeId = new NodeId(tag);

                MonitoredItem item = new MonitoredItem
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    DisplayName = tag,
                    SamplingInterval = this.MonitorInterval ?? DefaultMonitorInterval
                };
                sub.AddItem(item);

                item.Notification += (monitoredItem, args) =>
                {
                    MonitoredItemNotification monitoredItemNotification = (MonitoredItemNotification)args.NotificationValue;

                    ReadEvent monitorEvent = new ReadEvent
                    {
                        Value = monitoredItemNotification.Value.WrappedValue.Value,
                        SourceTimestamp = monitoredItemNotification.Value.SourceTimestamp,
                        ServerTimestamp = monitoredItemNotification.Value.ServerTimestamp
                    };
                    if (StatusCode.IsGood(monitoredItemNotification.Value.StatusCode)) monitorEvent.Quality = Quality.Good;
                    if (StatusCode.IsBad(monitoredItemNotification.Value.StatusCode)) monitorEvent.Quality = Quality.Bad;

                    string tagNo = monitoredItem.StartNodeId.ToString();
                    if (readEvents.ContainsKey(tagNo))
                    {
                        readEvents[tagNo] = monitorEvent;
                    }
                    else
                    {
                        readEvents.Add(tagNo, monitorEvent);
                    }

                    callback(readEvents, unsubscribe);
                };
            }

            this._session.AddSubscription(sub);
            sub.Create();
            sub.ApplyChanges();
        }
        #endregion

        #region Monitor the specified tags for changes —— void MonitorChanges(IEnumerable<string> tags...
        /// <summary>
        /// Monitor the specified tags for changes
        /// </summary>
        /// <param name="tags">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is the new values of the nodes, the second is an `unsubscribe` function to unsubscribe the callback</param>
        public void MonitorChanges(IEnumerable<string> tags, Action<IDictionary<string, ReadEvent>, Action> callback)
        {
            //Updated by Lee

            #region # Check

            tags = tags?.Distinct().ToArray() ?? new string[0];
            if (!tags.Any())
            {
                throw new ArgumentNullException(nameof(tags), "tags cannot be empty !");
            }

            #endregion

            Subscription sub = new Subscription
            {
                PublishingInterval = this.MonitorInterval ?? DefaultMonitorInterval,
                PublishingEnabled = true,
                LifetimeCount = this._options.SubscriptionLifetimeCount,
                KeepAliveCount = this._options.SubscriptionKeepAliveCount,
                DisplayName = string.Join(",", tags),
                Priority = byte.MaxValue
            };
            Action unsubscribe = () =>
            {
                sub.RemoveItems(sub.MonitoredItems);
                sub.Delete(true);
                this._session.RemoveSubscription(sub);
                sub.Dispose();
            };

            foreach (string tag in tags)
            {
                NodeId nodeId = new NodeId(tag);

                MonitoredItem item = new MonitoredItem
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    DisplayName = tag,
                    SamplingInterval = this.MonitorInterval ?? DefaultMonitorInterval
                };
                sub.AddItem(item);

                item.Notification += (monitoredItem, args) =>
                {
                    IDictionary<string, ReadEvent> readEvents = new ConcurrentDictionary<string, ReadEvent>();

                    MonitoredItemNotification monitoredItemNotification = (MonitoredItemNotification)args.NotificationValue;

                    ReadEvent monitorEvent = new ReadEvent
                    {
                        Value = monitoredItemNotification.Value.WrappedValue.Value,
                        SourceTimestamp = monitoredItemNotification.Value.SourceTimestamp,
                        ServerTimestamp = monitoredItemNotification.Value.ServerTimestamp
                    };
                    if (StatusCode.IsGood(monitoredItemNotification.Value.StatusCode)) monitorEvent.Quality = Quality.Good;
                    if (StatusCode.IsBad(monitoredItemNotification.Value.StatusCode)) monitorEvent.Quality = Quality.Bad;

                    string tagNo = monitoredItem.StartNodeId.ToString();
                    readEvents.Add(tagNo, monitorEvent);

                    callback(readEvents, unsubscribe);
                };
            }

            this._session.AddSubscription(sub);
            sub.Create();
            sub.ApplyChanges();
        }
        #endregion

        #region Releasing unmanaged resources —— void Dispose()
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this._session != null)
            {
                this._session.RemoveSubscriptions(this._session.Subscriptions.ToList());
                this._session.Close();
                this._session.Dispose();
                this.Status = OpcStatus.NotConnected;

                GC.SuppressFinalize(this);
            }
        }
        #endregion


        //Public

        #region Reconnect the OPC session —— void ReConnect()
        /// <summary>
        /// Reconnect the OPC session
        /// </summary>
        public void ReConnect()
        {
            this.Status = OpcStatus.NotConnected;
            this._session.Reconnect();
            this.Status = OpcStatus.Connected;
        }
        #endregion

        #region Create a new OPC session, based on the current session parameters —— void RecreateSession()
        /// <summary>
        /// Create a new OPC session, based on the current session parameters.
        /// </summary>
        public void RecreateSession()
        {
            this.Status = OpcStatus.NotConnected;
            this._session = Session.Recreate(this._session);
            this.Status = OpcStatus.Connected;
        }
        #endregion


        //Private

        #region Crappy method to initialize the session —— Session InitializeSession(Uri url)
        /// <summary>
        /// Crappy method to initialize the session. I don't know what many of these things do, sincerely.
        /// </summary>
        private Session InitializeSession(Uri url)
        {
            CertificateValidator certificateValidator = new CertificateValidator();
            certificateValidator.CertificateValidation += (sender, eventArgs) =>
            {
                if (ServiceResult.IsGood(eventArgs.Error))
                    eventArgs.Accept = true;
                else if ((eventArgs.Error.StatusCode.Code == StatusCodes.BadCertificateUntrusted) && this._options.AutoAcceptUntrustedCertificates)
                    eventArgs.Accept = true;
                else
                    throw new OpcException(string.Format("Failed to validate certificate with error code {0}: {1}", eventArgs.Error.Code, eventArgs.Error.AdditionalInfo), eventArgs.Error.StatusCode);
            };
            // Build the application configuration
            ApplicationInstance appInstance = new ApplicationInstance
            {
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = this._options.ConfigSectionName,
                ApplicationConfiguration = new ApplicationConfiguration
                {
                    ApplicationUri = url.ToString(),
                    ApplicationName = this._options.ApplicationName,
                    ApplicationType = ApplicationType.Client,
                    CertificateValidator = certificateValidator,
                    ServerConfiguration = new ServerConfiguration
                    {
                        MaxSubscriptionCount = this._options.MaxSubscriptionCount,
                        MaxMessageQueueSize = this._options.MaxMessageQueueSize,
                        MaxNotificationQueueSize = this._options.MaxNotificationQueueSize,
                        MaxPublishRequestCount = this._options.MaxPublishRequestCount
                    },
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = this._options.AutoAcceptUntrustedCertificates
                    },
                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 600000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 600000,
                        SecurityTokenLifetime = 3600000
                    },
                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000,
                        MinSubscriptionLifetime = 10000
                    },
                    DisableHiResClock = true
                }
            };

            // Assign a application certificate (when specified)
            if (this._options.ApplicationCertificate != null)
                appInstance.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier(this._options.ApplicationCertificate);

            // Find the endpoint to be used
            EndpointDescription endpoints = ClientUtils.SelectEndpoint(url, this._options.UseMessageSecurity);

            // Create the OPC session:
            Session session = Session.Create(
                configuration: appInstance.ApplicationConfiguration,
                endpoint: new ConfiguredEndpoint(
                    collection: null,
                    description: endpoints,
                    configuration: EndpointConfiguration.Create(applicationConfiguration: appInstance.ApplicationConfiguration)),
                updateBeforeConnect: false,
                checkDomain: false,
                sessionName: this._options.SessionName,
                sessionTimeout: this._options.SessionTimeout,
                identity: this.GetIdentity(url),
                preferredLocales: new string[] { });

            return session;
        }
        #endregion

        #region Return identity login object for a given URI —— UserIdentity GetIdentity(Uri url)
        /// <summary>
        /// Return identity login object for a given URI.
        /// </summary>
        /// <param name="url">Login URI</param>
        /// <returns>AnonUser or User with name and password</returns>
        private UserIdentity GetIdentity(Uri url)
        {
            if (this._options.UserIdentity != null)
            {
                return this._options.UserIdentity;
            }

            UserIdentity uriLogin = new UserIdentity();

            if (!string.IsNullOrEmpty(url.UserInfo))
            {
                string[] uis = url.UserInfo.Split(':');
                uriLogin = new UserIdentity(uis[0], uis[1]);
            }

            return uriLogin;
        }
        #endregion

        #region To build nodeId collection to read —— ReadValueIdCollection BuildReadValueIdCollection(...
        /// <summary>
        /// To build nodeId collection to read
        /// </summary>
        private ReadValueIdCollection BuildReadValueIdCollection(string tag, uint attributeId)
        {
            //Updated by Lee
            NodeId nodeId = new NodeId(tag);
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId()
                {
                    NodeId = nodeId,
                    AttributeId = attributeId
                }
            };

            return nodesToRead;
        }
        #endregion

        #region To build nodeId collection to write —— WriteValueCollection BuildWriteValueCollection(...
        /// <summary>
        /// To build nodeId collection to write
        /// </summary>
        private WriteValueCollection BuildWriteValueCollection(string tag, uint attributeId, object dataValue)
        {
            //Updated by Lee
            WriteValue valueToWrite = new WriteValue
            {
                NodeId = new NodeId(tag),
                AttributeId = attributeId,
                Value = { Value = dataValue }
            };

            WriteValueCollection valuesToWrite = new WriteValueCollection { valueToWrite };

            return valuesToWrite;
        }
        #endregion

        #region To check the return value —— void CheckReturnValue(StatusCode status)
        /// <summary>
        /// To check the return value
        /// </summary>
        private void CheckReturnValue(StatusCode status)
        {
            if (!StatusCode.IsGood(status))
            {
                throw new OpcException($"Invalid response from the server. (Response Status: {status})", status);
            }
        }
        #endregion


        //EventHandlers

        #region SessionKeepAlive event handler —— void OnSessionKeepAlive(Session session, KeepAliveEventArgs e)
        /// <summary>
        /// SessionKeepAlive event handler
        /// </summary>
        private void OnSessionKeepAlive(Session session, KeepAliveEventArgs e)
        {
            if (e.CurrentState != ServerState.Running)
            {
                if (this.Status == OpcStatus.Connected)
                {
                    this.Status = OpcStatus.NotConnected;
                    this.ServerConnectionLost?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (e.CurrentState == ServerState.Running)
            {
                if (this.Status == OpcStatus.NotConnected)
                {
                    this.Status = OpcStatus.Connected;
                    this.ServerConnectionRestored?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        #endregion

        #region SessionClosing event handler —— void OnSessionClosing(object sender, EventArgs e)
        /// <summary>
        /// SessionClosing event handler
        /// </summary>
        private void OnSessionClosing(object sender, EventArgs e)
        {
            this.Status = OpcStatus.NotConnected;
            this.ServerConnectionLost?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #endregion
    }

}
