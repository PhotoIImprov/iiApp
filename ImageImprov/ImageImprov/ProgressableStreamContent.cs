using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Diagnostics;  // for debug assertions.

namespace ImageImprov {
    // deriving from HttpContent means I have to import an non-compatible library
    // deriving from stream content fails.
    public class ProgressableStreamContent : ByteArrayContent {
        /// <summary>
        /// Lets keep buffer of 20kb
        /// </summary>
        private const int defaultBufferSize = 5 * 4096;

        private HttpContent content;
        private int bufferSize;
        //private bool contentConsumed;
        private Action<long, long> progress;

        //public ProgressableStreamContent(HttpContent content, Action<long, long> progress) : this(content, defaultBufferSize, progress) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="bufferSize"></param>
        /// <param name="progress"></param>
        /// <param name="dummyStream">We inherit from StreamContent as it circumvents adding a library to the Droid project that is incompatible.
        /// We pass in a dummyStream to keep the streamContent constructor happy. That stream is irrelevant because we override SerializeToStreamAsync.</param>
        public ProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress, Byte[] bytes) : base(bytes) {
            if (content == null) {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this.content = content;
            this.bufferSize = bufferSize;
            this.progress = progress;

            foreach (var h in content.Headers) {
                this.Headers.Add(h.Key, h.Value);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) {
            Debug.WriteLine("DHB:ProgressableStreamContent:SerializeToStreamAsync begin");
            Debug.WriteLine("DHB:ProgressableStreamContent:SerializeToStreamAsync stream:" +stream.ToString());
            return Task.Run(async () => {
                Debug.WriteLine("DHB:ProgressableStreamContent:SerializeToStreamAsync in task");
                var buffer = new Byte[this.bufferSize];
                long size;
                TryComputeLength(out size);
                var uploaded = 0;

                using (var sinput = await content.ReadAsStreamAsync()) {
                    while (true) {
                        var length = sinput.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        //downloader.Uploaded = uploaded += length;
                        uploaded += length;
                        progress?.Invoke(uploaded, size);

                        //System.Diagnostics.Debug.WriteLine($"Bytes sent {uploaded} of {size}");

                        stream.Write(buffer, 0, length);
                        stream.Flush();
                    }
                }
                stream.Flush();
            });
            Debug.WriteLine("DHB:ProgressableStreamContent:SerializeToStreamAsync end");
        }

        protected override bool TryComputeLength(out long length) {
            length = content.Headers.ContentLength.GetValueOrDefault();
            return true;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
