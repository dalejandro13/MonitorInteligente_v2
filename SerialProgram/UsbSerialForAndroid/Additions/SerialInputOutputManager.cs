﻿//
// Copyright 2014 LusoVU. All rights reserved.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301,
// USA.
// 
// Project home page: https://bitbucket.com/lusovu/xamarinusbserial
// 

using System;
using System.Threading;
using Android.Hardware.Usb;
using Android.Util;
using Hoho.Android.UsbSerial.Driver;
using System.Threading.Tasks;
using Android.Widget;
using Android.Content;

namespace Hoho.Android.UsbSerial.Util
{
	public class SerialInputOutputManager : IDisposable
	{
		static readonly string TAG = typeof(SerialInputOutputManager).Name;
		const int READ_WAIT_MILLIS = 20000;
		const int DEFAULT_BUFFERSIZE = 4096;
		const int DEFAULT_BAUDRATE = 115200;
		const int DEFAULT_DATABITS = 8;
		const Parity DEFAULT_PARITY = Parity.None;
		const StopBits DEFAULT_STOPBITS = StopBits.One;
        public Context context;
		readonly IUsbSerialPort port;
		object syncState = new object();
		byte[] buffer;
		CancellationTokenSource cancelationTokenSource;
		bool isOpen;

        public string mensaje { get; set; }
        byte[] datos;
        public int len2 { get; set; }


        public SerialInputOutputManager (IUsbSerialPort port)
		{
			this.port = port;
			BaudRate = DEFAULT_BAUDRATE;
			Parity = DEFAULT_PARITY;
			DataBits = DEFAULT_DATABITS;
			StopBits = DEFAULT_STOPBITS;
		}

		public int BaudRate { get; set; }

		public Parity Parity { get; set; }

		public int DataBits { get; set; }

		public StopBits StopBits { get; set; }

		public event EventHandler<SerialDataReceivedArgs> DataReceived;

		public event EventHandler<UnhandledExceptionEventArgs> ErrorReceived;

		public void Open (UsbManager usbManager, int bufferSize = DEFAULT_BUFFERSIZE)
		{
			if (disposed)
				throw new ObjectDisposedException (GetType().Name);
			if (IsOpen)
				throw new InvalidOperationException ();

			var connection = usbManager.OpenDevice (port.Driver.Device);
			if (connection == null)
				throw new Java.IO.IOException ("Failed to open device");
			isOpen = true;

			buffer = new byte[bufferSize];
			port.Open (connection);
			port.SetParameters(BaudRate, DataBits, StopBits, Parity);

			cancelationTokenSource = new CancellationTokenSource ();
			var cancelationToken = cancelationTokenSource.Token;
			cancelationToken.Register (() => Log.Info (TAG, "Cancellation Requested"));

			Task.Run (() => {
				Log.Info (TAG, "Task Started!");
				try {
					while(true)
					{
                        Thread.Sleep(4000); //sleep new line
						cancelationToken.ThrowIfCancellationRequested();
                        sending(); //sending data new line
						Step(); // execute step
					}
				}
				catch(OperationCanceledException) {
					throw;
				}
				catch (Exception e) {
					Log.Warn (TAG, "Task ending due to exception: " + e.Message, e);
					ErrorReceived.Raise (this, new UnhandledExceptionEventArgs (e, false));
				} finally {
					port.Close ();
					buffer = null;
					isOpen = false;
					Log.Info (TAG, "Task Ended!");
				}
			}, cancelationToken);
		}

        public void Close()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().Name);
			if (!IsOpen)
				throw new InvalidOperationException();

			// cancel task
			cancelationTokenSource.Cancel ();
		}

		public bool IsOpen { 
			get {
				return isOpen;
			}
		}

        /////////////////////new code///////////////////////////
        private void sending()
        {
            mensaje = "hola como estas";
            datos = System.Text.Encoding.UTF8.GetBytes(mensaje);
            len2 = port.Write(datos, READ_WAIT_MILLIS);
        }
        /////////////////////////////////////////////////////////

        void Step()
        {
            // handle incoming data.
            var len = port.Read(buffer, READ_WAIT_MILLIS);
			if (len > 0) {
				Log.Debug(TAG, "Read data len=" + len);

				var data = new byte[len];
				Array.Copy(buffer, data, len);
				DataReceived.Raise(this, new SerialDataReceivedArgs (data));
			}
		}

		#region Dispose pattern implementation

		bool disposed;

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing) {
				Close ();
			}

			disposed = true;
		}

		~SerialInputOutputManager()
		{
			Dispose (false);
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		#endregion

		#endregion
	}
}

