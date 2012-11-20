/*
 * Author: Rodolfo Finochietti
 * Email: rfinochi@shockbyte.net
 * Web: http://shockbyte.net
 *
 * This work is licensed under the Creative Commons Attribution License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by/2.0
 * or send a letter to Creative Commons, 559 Nathan Abbott Way, Stanford, California 94305, USA.
 * 
 * No warranties expressed or implied, use at your own risk.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
#if NET45  
using System.Threading.Tasks;
#endif

using Pop3.IO;

namespace Pop3
{
    public class Pop3Client : IDisposable
    {
        #region Private Fields

        private INetworkOperations _networkOperations;

        #endregion

        #region Constructors

        public Pop3Client( )
        {
            _networkOperations = new TcpNetworkOperations( );
        }

        public Pop3Client( INetworkOperations networkOperations )
        {
            if ( networkOperations == null )
                throw new ArgumentNullException( "networkOperations", "The parameter networkOperation can't be null" );

            _networkOperations = networkOperations;
        }

        #endregion

        #region Properties

        public bool IsConnected
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void Connect( string server, string userName, string password )
        {
            Connect( server, userName, password, 110, false );
        }

        public void Connect( string server, string userName, string password, bool useSsl )
        {
            Connect( server, userName, password, ( useSsl ? 995 : 110 ), useSsl );
        }

        public void Connect( string server, string userName, string password, int port, bool useSsl )
        {
            if ( this.IsConnected )
                throw new Pop3Exception( "Pop3 client already connected" );

            _networkOperations.Open( server, port, useSsl );

            string response = _networkOperations.Read( );
            if ( String.IsNullOrEmpty ( response ) || response.Substring( 0, 3 ) != "+OK" )
                throw new Pop3Exception( response );

            SendCommand( String.Format( CultureInfo.InvariantCulture, "USER {0}", userName ) );
            SendCommand( String.Format( CultureInfo.InvariantCulture, "PASS {0}", password ) );

            this.IsConnected = true;
        }

        public void Disconnect( )
        {
            if ( !this.IsConnected )
                return;

            try
            {
                SendCommand( "QUIT" );
                _networkOperations.Close( );
            }
            finally
            {
                this.IsConnected = false;
            }
        }

        public Collection<Pop3Message> List( )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            Collection<Pop3Message> result = new Collection<Pop3Message>( );

            SendCommand( "LIST" );

            while ( true )
            {
                string response = _networkOperations.Read( );
                if ( response == ".\r\n" )
                    return result;

                Pop3Message message = new Pop3Message( );

                char[] seps = { ' ' };
                string[] values = response.Split( seps );

                message.Number = Int32.Parse( values[ 0 ], CultureInfo.InvariantCulture );
                message.Bytes = Int32.Parse( values[ 1 ], CultureInfo.InvariantCulture );
                message.Retrieved = false;

                result.Add( message );
            }
        }

        public void RetrieveHeader( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            if ( message == null )
                throw new ArgumentNullException( "message" );

            SendCommand( "TOP", "0", message );

            while ( true )
            {
                string response = _networkOperations.Read( );
                if ( response == ".\r\n" )
                    break;

                message.RawHeader += response;
            }
        }

        public void RetrieveHeader( IEnumerable<Pop3Message> messages )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( messages == null )
                throw new ArgumentNullException( "messages" );

            foreach ( Pop3Message message in messages )
                RetrieveHeader( message );
        }

        public void Retrieve( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( message == null )
                throw new ArgumentNullException( "message" );

            SendCommand( "RETR", message );

            while ( true )
            {
                string response = _networkOperations.Read( );
                if ( response == ".\r\n" )
                    break;

                message.RawMessage += response;
            }
            message.Retrieved = true;
        }

        public void Retrieve( IEnumerable<Pop3Message> messages )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( messages == null )
                throw new ArgumentNullException( "messages" );

            foreach ( Pop3Message message in messages )
                Retrieve( message );
        }

        public Collection<Pop3Message> ListAndRetrieveHeader( )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            Collection<Pop3Message> messages = List( );

            RetrieveHeader( messages );

            return messages;
        }

        public Collection<Pop3Message> ListAndRetrieve( )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            Collection<Pop3Message> messages = List( );

            Retrieve( messages );

            return messages;
        }

        public void Delete( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( message == null )
                throw new ArgumentNullException( "message" );

            SendCommand( "DELE", message );
        }

        #endregion

        #region Public Async Methods

#if NET45  
        public async Task ConnectAsync( string server, string userName, string password )
        {
            await ConnectAsync( server, userName, password, 110, false );
        }

        public async Task ConnectAsync( string server, string userName, string password, bool useSsl )
        {
            await ConnectAsync( server, userName, password, ( useSsl ? 995 : 110 ), useSsl );
        }

        public async Task ConnectAsync( string server, string userName, string password, int port, bool useSsl )
        {
            if ( this.IsConnected )
                throw new Pop3Exception( "Pop3 client already connected" );

            await _networkOperations.OpenAsync( server, port, useSsl );

            string response = await _networkOperations.ReadAsync( );
            if ( String.IsNullOrEmpty( response ) || response.Substring( 0, 3 ) != "+OK" )
                throw new Pop3Exception( response );

            await SendCommandAsync( String.Format( CultureInfo.InvariantCulture, "USER {0}", userName ) );
            await SendCommandAsync( String.Format( CultureInfo.InvariantCulture, "PASS {0}", password ) );

            this.IsConnected = true;
        }
        
        public async Task DisconnectAsync( )
        {
            if ( !this.IsConnected )
                return;

            try
            {
                await SendCommandAsync( "QUIT" );
                _networkOperations.Close( );
            }
            finally
            {
                this.IsConnected = false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures" )]
        public async Task<Collection<Pop3Message>> ListAsync( )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            Collection<Pop3Message> result = new Collection<Pop3Message>( );

            await SendCommandAsync( "LIST" );

            while ( true )
            {
                string response = await _networkOperations.ReadAsync( );
                if ( response == ".\r\n" )
                    return result;

                Pop3Message message = new Pop3Message( );

                char[] seps = { ' ' };
                string[] values = response.Split( seps );

                message.Number = Int32.Parse( values[ 0 ], CultureInfo.InvariantCulture );
                message.Bytes = Int32.Parse( values[ 1 ], CultureInfo.InvariantCulture );
                message.Retrieved = false;

                result.Add( message );
            }
        }

        public async Task RetrieveHeaderAsync( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            if ( message == null )
                throw new ArgumentNullException( "message" );

            await SendCommandAsync( "TOP", "0", message );

            while ( true )
            {
                string response = await _networkOperations.ReadAsync( );
                if ( response == ".\r\n" )
                    break;

                message.RawHeader += response;
            }
        }

        public async Task RetrieveHeaderAsync( IEnumerable<Pop3Message> messages )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( messages == null )
                throw new ArgumentNullException( "messages" );

            foreach ( Pop3Message message in messages )
                await RetrieveHeaderAsync( message );      
        }

        public async Task RetrieveAsync( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( message == null )
                throw new ArgumentNullException( "message" );

            await SendCommandAsync( "RETR", message );

            while ( true )
            {
                string response = await _networkOperations.ReadAsync( );
                if ( response == ".\r\n" )
                    break;

                message.RawMessage += response;
            }
            message.Retrieved = true;
        }

        public async Task RetrieveAsync( IEnumerable<Pop3Message> messages )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( messages == null )
                throw new ArgumentNullException( "messages" );

            foreach ( Pop3Message message in messages )
                await RetrieveAsync( message );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures" )]
        public async Task<Collection<Pop3Message>> ListAndRetrieveHeaderAsync( )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            Collection<Pop3Message> messages = await ListAsync( );

            await RetrieveHeaderAsync( messages );

            return messages;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures" )]
        public async Task<Collection<Pop3Message>> ListAndRetrieveAsync( )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );

            Collection<Pop3Message> messages = await ListAsync( );

            await RetrieveAsync( messages );

            return messages;
        }
        
        public async Task DeleteAsync( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new Pop3Exception( "Pop3 client is not connected to host" );
            if ( message == null )
                throw new ArgumentNullException( "message" );

            await SendCommandAsync( "DELE", message );
        }
#endif

        #endregion
        
        #region Private Methods

        private void SendCommand( string command, Pop3Message message )
        {
            SendCommand( command, null, message );
        }

        private void SendCommand( string command, string aditionalParameters = null, Pop3Message message = null )
        {
            var request = new StringBuilder( );

            if ( message == null )
                request.AppendFormat( CultureInfo.InvariantCulture, "{0}", command );
            else
                request.AppendFormat( CultureInfo.InvariantCulture, "{0} {1}", command, message.Number );

            if ( !String.IsNullOrEmpty( aditionalParameters ) )
                request.AppendFormat( " {0}", aditionalParameters );

            request.Append( "\r\n" );

            _networkOperations.Write( request.ToString( ) );

            var response = _networkOperations.Read( );

            if ( String.IsNullOrEmpty( response ) || response.Substring( 0, 3 ) != "+OK" )
                throw new Pop3Exception( response );
        }

        #endregion

        #region Private Async Methods

#if NET45
        private async Task SendCommandAsync( string command, Pop3Message message )
        {
            await SendCommandAsync( command, null, message );
        }

        private async Task SendCommandAsync( string command, string aditionalParameters = null, Pop3Message message = null )
        {
            var request = new StringBuilder( );

            if ( message == null )
                request.AppendFormat( CultureInfo.InvariantCulture, "{0}", command );
            else
                request.AppendFormat( CultureInfo.InvariantCulture, "{0} {1}", command, message.Number );

            if ( !String.IsNullOrEmpty( aditionalParameters ) )
                request.AppendFormat( " {0}", aditionalParameters );

            request.Append( "\r\n" );

            await _networkOperations.WriteAsync( request.ToString( ) );

            var response = await _networkOperations.ReadAsync( );

            if ( String.IsNullOrEmpty( response ) || response.Substring( 0, 3 ) != "+OK" )
                throw new Pop3Exception( response );
        }
#endif

        #endregion

        #region Dispose-Finalize Pattern

        public void Dispose( )
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        ~Pop3Client( )
        {
            Dispose( false );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if ( _networkOperations != null )
                {
                    _networkOperations.Close( );
                    _networkOperations.Dispose( );
                    _networkOperations = null;
                }
            }

        }

        #endregion
    }
}