using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hylasoft.Opc.Common
{
    /// <summary>
    /// OPC Client Interface
    /// </summary>
    public interface IOpcClient : IDisposable
    {
        #region # Properties

        #region Monitor interval in Milliseconds —— int? MonitorInterval
        /// <summary>
        /// Monitor interval in Milliseconds
        /// </summary>
        int? MonitorInterval { get; set; }
        #endregion

        #region Gets the current status of the OPC Client —— OpcStatus Status { get; }
        /// <summary>
        /// Gets the current status of the OPC Client
        /// </summary>
        OpcStatus Status { get; }
        #endregion

        #endregion

        #region # Methods

        #region Connect the client to the OPC Server —— void Connect()
        /// <summary>
        /// Connect the client to the OPC Server
        /// </summary>
        void Connect();
        #endregion

        #region Gets the datatype of an OPC tag —— Type GetDataType(string tag)
        /// <summary>
        /// Gets the datatype of an OPC tag
        /// </summary>
        /// <param name="tag">Tag to get datatype of</param>
        /// <returns>System Type</returns>
        Type GetDataType(string tag);
        #endregion

        #region Read a tag —— ReadEvent Read(string tag)
        /// <summary>
        /// Read a tag
        /// </summary>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        ReadEvent Read(string tag);
        #endregion

        #region Read a tag —— ReadEvent<T> Read<T>(string tag)
        /// <summary>
        /// Read a tag
        /// </summary>
        /// <typeparam name="T">The type of tag to read</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        ReadEvent<T> Read<T>(string tag);
        #endregion

        #region Read a tag asynchronusly —— Task<ReadEvent<T>> ReadAsync<T>(string tag)
        /// <summary>
        /// Read a tag asynchronusly
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an async method.")]
        Task<ReadEvent<T>> ReadAsync<T>(string tag);
        #endregion

        #region Write a value on the specified opc tag —— void Write<T>(string tag, T item)
        /// <summary>
        /// Write a value on the specified opc tag
        /// </summary>
        /// <typeparam name="T">The type of tag to write on</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` writes on the tag `bar` on the folder `foo`</param>
        /// <param name="item"></param>
        void Write<T>(string tag, T item);
        #endregion

        #region Write a value on the specified opc tag asynchronously —— Task WriteAsync<T>(string tag, T item)
        /// <summary>
        /// Write a value on the specified opc tag asynchronously
        /// </summary>
        Task WriteAsync<T>(string tag, T item);
        #endregion

        #region Monitor the specified tag for changes —— void Monitor(string tag, Action<ReadEvent, Action> callback)
        /// <summary>
        /// Monitor the specified tag for changes
        /// </summary>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is the new value of the node, the second is an `unsubscribe` function to unsubscribe the callback</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an async method.")]
        void Monitor(string tag, Action<ReadEvent, Action> callback);
        #endregion

        #region Monitor the specified tag for changes —— void Monitor<T>(string tag, Action<ReadEvent<T>, Action> callback)
        /// <summary>
        /// Monitor the specified tag for changes
        /// </summary>
        /// <typeparam name="T">the type of tag to monitor</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is the new value of the node, the second is an `unsubscribe` function to unsubscribe the callback</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an async method.")]
        void Monitor<T>(string tag, Action<ReadEvent<T>, Action> callback);
        #endregion

        #region Monitor the specified tags for changes —— void Monitor(IEnumerable<string> tags...
        /// <summary>
        /// Monitor the specified tags for changes
        /// </summary>
        /// <param name="tags">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is the new values of the nodes, the second is an `unsubscribe` function to unsubscribe the callback</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an async method.")]
        void Monitor(IEnumerable<string> tags, Action<IDictionary<string, ReadEvent>, Action> callback);
        #endregion

        #endregion
    }
}