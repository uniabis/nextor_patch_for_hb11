using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NextorPatcherForHB11

{
    public class NextorPatcherForHB11Parameter
    {
        public bool Valid { get; private set; }
        public string InputPath { get; private set; }
        public string OutputPath { get; private set; }
        public NextorPatcherForHB11Parameter( string[] argv )
        {
            InputPath = string.Empty;
            OutputPath = string.Empty;
            Valid = Parse( argv );
        }
        private bool Parse( string[] argv )
        {
            if ( argv == null )
            {
                //return false;
                throw new ArgumentNullException( nameof( argv ) );
            }
            for ( int i = 0; i < argv.Length; i++ )
            {
                if ( argv[ i ].ToLower() == "-i"
                    && i + 1 < argv.Length )
                {
                    InputPath = argv[ i + 1 ];
                }
                else if ( argv[ i ].ToLower() == "-o"
                    && i + 1 < argv.Length )
                {
                    OutputPath = argv[ i + 1 ];
                }
            }
            bool result = !string.IsNullOrEmpty( InputPath )
                && File.Exists( InputPath );
            return result;
        }
    }

    public class NextorPatcherForHB11
    {
        private const int NEXTOR_KERNEL_BASE_ADDRSS = 0x4000;
        private const int NEXTOR_KERNEL_VERSION_OFFSET = 0x11E;
        private const string NEXTER_KERNEL_VERSION_MAGIC = "Nextor kernel version";

        private static byte[] HB11_DISKBASIC_ENTRY_CODE = new byte[] { 0x3A, 0xB1, 0xFB, 0xB7, 0xDD, 0x21, 0xE9, 0x7D };
        private static byte[] HB11_HRUNC__HANDLING_CODE = new byte[] { 0x21, 0xCB, 0xFE, 0x36, 0xF7, 0x23, 0x77, 0x23, 0x11 };

        private const int HB11_DISKBASIC_ENTRY_ADDRESS = 0x59DB;
        private const int HB11_DISKBASIC_PATCH1_ADDRESS = 0x76F0;
        private const int HB11_DISKBASIC_PATCH2_ADDRESS = 0x76A0;

        private static byte[] NEXTOR_2_1_0_BETA1_PATTERN = new byte[] { 0xBE, 0x20, 0x13, 0x13, 0x23, 0xA7, 0x20, 0xF7 };
        private static byte[] NEXTOR_2_1_0_BETA1_PATCH = new byte[] { 0xBE, 0xC2, 0xED, 0x59, 0x13, 0x23, 0xA7, 0xC2, 0xD6, 0x59, 0XC3, 0xDF, 0x59 };

        private const string MFR_RECOVERY_MAGIC = "MFRSD KERNEL 1.0";


        private NextorPatcherForHB11Parameter Parameters { get; set; }

        internal string Log
        {
            get
            {
                return log.ToString();
            }
        }
        private StringBuilder log;
        private byte[] buf;
        private byte[] mfr;

        public NextorPatcherForHB11( NextorPatcherForHB11Parameter param )
        {
            Parameters = param;
            log = new StringBuilder();
            buf = new byte[ 0 ];
            mfr = new byte[ 0 ];
        }

        private static int IndexOf<T>( T[] target, T[] key, int offset )
        {
            if ( target == null )
            {
                throw new ArgumentNullException( nameof( target ) );
            }
            if ( key == null )
            {
                throw new ArgumentNullException( nameof( key ) );
            }
            for ( int i = offset; i <= target.Length; i++ )
            {
                bool unmatch = true;
                if ( i + key.Length <= target.Length )
                {
                    for ( int j = 0; j < key.Length; j++ )
                    {
                        if ( !( target[ i + j ]?.Equals( key[ j ] ) ?? false ) )
                        {
                            unmatch = true;
                            break;
                        }
                        unmatch = false;
                    }
                }
                if ( !unmatch )
                {
                    return i;
                }
            }
            return -1;
        }

        private string GetNexterVersionText()
        {
            //var encoding = Encoding.GetEncoding(932);
            var encoding = Encoding.Default;

            var kernelVersionLength = buf.Length < NEXTOR_KERNEL_VERSION_OFFSET
                ? -1
                : ( Array.IndexOf( buf, ( byte )0, NEXTOR_KERNEL_VERSION_OFFSET ) - NEXTOR_KERNEL_VERSION_OFFSET );
            string kernelVersionText = kernelVersionLength < 0
                ? string.Empty
                : encoding.GetString( buf, NEXTOR_KERNEL_VERSION_OFFSET, kernelVersionLength ).Trim();

            if ( kernelVersionLength < 0 || !kernelVersionText.StartsWith( NEXTER_KERNEL_VERSION_MAGIC ) )
            {
                return string.Empty;
            }
            return kernelVersionText;
        }

        private bool CheckPatchAreaEmpty( int address, int length )
        {
            bool chkempty = true;
            byte chkcode = buf[ address - NEXTOR_KERNEL_BASE_ADDRSS + 0 ];

            if ( chkcode != 0x00 && chkcode != 0xFF )
            {
                chkempty = false;
            }
            else
            {
                for ( int i = 1; i < length; i++ )
                {
                    if ( buf[ address - NEXTOR_KERNEL_BASE_ADDRSS + i ] != chkcode )
                    {
                        chkempty = false;
                        break;
                    }
                }
            }

            return chkempty;
        }

        private bool ReadFile()
        {
            try
            {
                //var encoding = Encoding.GetEncoding(932);
                var encoding = Encoding.Default;

                const int mfr_header_size = 512;
                const int mfr_kernel_size = 8 * 16 * 1024;
                mfr = new byte[ 0 ];
                buf = File.ReadAllBytes( Parameters.InputPath ?? string.Empty );
                if ( buf.Length == mfr_header_size + mfr_kernel_size )
                {
                    var header = buf.ToList().GetRange( 0, mfr_header_size ).ToArray();
                    var magic = encoding.GetBytes( MFR_RECOVERY_MAGIC );
                    var ismfr = IndexOf( header, magic, 0 ) == 0;
                    if ( ismfr )
                    {
                        mfr = header;
                        buf = buf.ToList().GetRange( mfr_header_size, mfr_kernel_size ).ToArray();
                    }
                    else
                    {
                        log.AppendLine( $"Unknown file:{Parameters.InputPath}" );
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                log.AppendLine( $"File read error:{Parameters.InputPath}" );
                return false;
            }
        }

        private bool WriteFile()
        {
            var outputPath = Parameters.OutputPath;
            try
            {
                if ( string.IsNullOrEmpty( outputPath ) )
                {
                    var inputPath = Parameters.InputPath;
                    var dir = Path.GetDirectoryName( inputPath ) ?? string.Empty;
                    var srcfn = Path.GetFileNameWithoutExtension( inputPath ) ?? string.Empty;
                    var ext = Path.GetExtension( inputPath ) ?? string.Empty;
                    var dstfn = srcfn + ".hb11patch" + ext;
                    outputPath = Path.Combine( dir, dstfn );
                }
                if ( mfr.Length > 0 )
                {
                    File.WriteAllBytes( outputPath, mfr.Concat( buf ).ToArray() );
                }
                else
                {
                    File.WriteAllBytes( outputPath, buf );
                }
            }
            catch
            {
                log.AppendLine( $"File write error:{outputPath}" );
                return false;
            }
            return true;
        }

        private byte[] GetPatchBinary( int index )
        {
            var asm = Assembly.GetExecutingAssembly();
            var names = asm.GetManifestResourceNames();
            using ( var stream = asm.GetManifestResourceStream( names[ index ] ) )
            {
                var result = new byte[ stream?.Length ?? 0 ];
                stream?.Read( result, 0, result.Length );
                return result;
            }
        }

        private bool Patch()
        {
            var patch2 = GetPatchBinary( 0 );

            if ( !CheckPatchAreaEmpty( HB11_DISKBASIC_PATCH1_ADDRESS, 0x10 ) )
            {
                log.AppendLine( $"HB-11 PATCH AREA{HB11_DISKBASIC_PATCH1_ADDRESS.ToString( "X4" )}:already used" );
                return false;
            }

            if ( !CheckPatchAreaEmpty( HB11_DISKBASIC_PATCH2_ADDRESS, patch2.Length ) )
            {
                log.AppendLine( $"HB-11 PATCH AREA{HB11_DISKBASIC_PATCH2_ADDRESS.ToString( "X4" )}:already used" );
                return false;
            }

            int hb11entryaddress = IndexOf( buf, HB11_DISKBASIC_ENTRY_CODE, 0 ) + NEXTOR_KERNEL_BASE_ADDRSS;

            if ( hb11entryaddress < NEXTOR_KERNEL_BASE_ADDRSS )
            {
                log.AppendLine( "DISK BASIC ENTRY CODE for HB-11:not found" );
                return false;
            }

            int hrunchandlingaddress = IndexOf( buf, HB11_HRUNC__HANDLING_CODE, 0 ) + NEXTOR_KERNEL_BASE_ADDRSS;

            if ( hrunchandlingaddress < NEXTOR_KERNEL_BASE_ADDRSS )
            {
                log.AppendLine( "H.RUNC HANDLING CODE:not found" );
                return false;
            }

            int hrunchandleraddress = ( buf[ hrunchandlingaddress - NEXTOR_KERNEL_BASE_ADDRSS + HB11_HRUNC__HANDLING_CODE.Length + 1 ] << 8 )
                | buf[ hrunchandlingaddress - NEXTOR_KERNEL_BASE_ADDRSS + HB11_HRUNC__HANDLING_CODE.Length + 0 ];

            if ( IndexOf( buf, NEXTOR_2_1_0_BETA1_PATTERN, HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 4 ) == HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 4 )
            {
                // 2.1.0 beta1
                Array.Copy( NEXTOR_2_1_0_BETA1_PATCH, 0, buf, HB11_DISKBASIC_PATCH1_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS, NEXTOR_2_1_0_BETA1_PATCH.Length );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 4 ] = 0xC3;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 ] = ( byte )( ( HB11_DISKBASIC_PATCH1_ADDRESS >> 0 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 2 ] = ( byte )( ( HB11_DISKBASIC_PATCH1_ADDRESS >> 8 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 1 ] = 0x00;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 0 ] = 0xC3;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 1 ] = ( byte )( ( hb11entryaddress >> 0 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 2 ] = ( byte )( ( hb11entryaddress >> 8 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 3 ] = 0x00;

                log.AppendLine( "HB-11 PATCH MODE:Nextor 2.1.0 beta1" );
            }
            else if ( buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 4 ] == 0xE1
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 ] == 0xCD
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 0 ] == 0xCD
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 3 ] == 0xA7
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 4 ] == 0xC9 )
            {
                // 2.1.0 beta2
                // 2.1.0 rc1
                for ( int i = 0; i < 8; i++ )
                {
                    buf[ HB11_DISKBASIC_PATCH1_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + i ] = buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 + i ];
                }

                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 ] = 0xC3;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 2 ] = ( byte )( ( HB11_DISKBASIC_PATCH1_ADDRESS >> 0 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 1 ] = ( byte )( ( HB11_DISKBASIC_PATCH1_ADDRESS >> 8 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 0 ] = 0xC3;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 1 ] = ( byte )( ( hb11entryaddress >> 0 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 2 ] = ( byte )( ( hb11entryaddress >> 8 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 3 ] = 0x00;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 4 ] = 0x00;

                log.AppendLine( "HB-11 PATCH MODE:Nextor 2.1.0 beta2" );
            }
            else if ( buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 ] == 0xE1
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 2 ] == 0xCD
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 1 ] == 0xCD
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 4 ] == 0xA7
                && buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 5 ] == 0xC9 )
            {
                // 2.1.0
                // 2.1.1 alpha1
                // 2.1.1 alpha2
                // 2.1.1 beta1
                // 2.1.1 beta2
                for ( int i = 0; i < 9; i++ )
                {
                    buf[ HB11_DISKBASIC_PATCH1_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + i ] = buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 + i ];
                }

                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3 ] = 0xC3;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 2 ] = ( byte )( ( HB11_DISKBASIC_PATCH1_ADDRESS >> 0 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 1 ] = ( byte )( ( HB11_DISKBASIC_PATCH1_ADDRESS >> 8 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 0 ] = 0xC3;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 1 ] = ( byte )( ( hb11entryaddress >> 0 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 2 ] = ( byte )( ( hb11entryaddress >> 8 ) & 255 );
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 3 ] = 0x00;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 4 ] = 0x00;
                buf[ HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 5 ] = 0x00;

                log.AppendLine( "HB-11 PATCH MODE:Nextor 2.1.0" );
            }
            else
            {
                log.AppendLine( "HB-11 PATCH MODE:unknown kernel" );
                return false;
            }

            Array.Copy( patch2, 0, buf, HB11_DISKBASIC_PATCH2_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS, patch2.Length );
            buf[ HB11_DISKBASIC_PATCH2_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 1 ] = ( byte )( ( hrunchandleraddress >> 0 ) & 255 );
            buf[ HB11_DISKBASIC_PATCH2_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS + 2 ] = ( byte )( ( hrunchandleraddress >> 8 ) & 255 );
            buf[ hrunchandlingaddress - NEXTOR_KERNEL_BASE_ADDRSS + HB11_HRUNC__HANDLING_CODE.Length + 0 ] = ( byte )( ( HB11_DISKBASIC_PATCH2_ADDRESS >> 0 ) & 255 );
            buf[ hrunchandlingaddress - NEXTOR_KERNEL_BASE_ADDRSS + HB11_HRUNC__HANDLING_CODE.Length + 1 ] = ( byte )( ( HB11_DISKBASIC_PATCH2_ADDRESS >> 8 ) & 255 );

            log.AppendLine( $"ADDRESS OF DISK BASIC ENTRY CODE:{hb11entryaddress.ToString( "X4" )}" );
            log.AppendLine( $"ADDRESS OF H.RUNC HANDLING CODE:{hrunchandlingaddress.ToString( "X4" )}" );
            log.AppendLine( $"ADDRESS OF H.RUNC HANDLER CODE:{hrunchandleraddress.ToString( "X4" )}" );

            return true;
        }

        public bool Run()
        {

            if ( !Parameters.Valid )
            {
                return false;
            }

            if ( !ReadFile() )
            {
                return false;
            }

            string kernelVersionText = GetNexterVersionText();
            if ( string.IsNullOrEmpty( kernelVersionText ) )
            {
                log.AppendLine( "Kernel version:not found" );
                return false;
            }
            log.AppendLine( $"Kernel version:{kernelVersionText}" );

            if ( !Patch() )
            {
                return false;
            }

            if ( !WriteFile() )
            {
                return false;
            }

            return true;
        }
    }


    internal static class NextorPatcherForHB11ConsoleEntry
    {
        public static int Main( string[] argv )
        {
#if NETCOREAPP
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            const string title = "NextorPatch For HB-11 version 0.0.1\n";

            string exe = ( Assembly.GetEntryAssembly()?.FullName ?? string.Empty )
                .Split( ',' ).First();
            string help = $"usage: {exe} -i {{input path}} [-o {{output path}}]\n";

            Console.Write( title );

            var param = new NextorPatcherForHB11Parameter( argv );
            var patcher = new NextorPatcherForHB11( param );
            bool result = patcher.Run();
            var log = patcher.Log;

            if ( !result )
            {
                Console.Write( help );
            }
            if ( !string.IsNullOrEmpty( log ) )
            {
                Console.Write( log );
            };

            return result ? 0 : 1;
        }
    }
}
