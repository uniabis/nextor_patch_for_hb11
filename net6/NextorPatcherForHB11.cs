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
        public bool DisableUnmusicPatch { get; private set; }
        public NextorPatcherForHB11Parameter( string[] argv )
        {
            InputPath = string.Empty;
            OutputPath = string.Empty;
            DisableUnmusicPatch = false;
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
                if ( ( argv[ i ].ToLower() == "-i"
                    || argv[ i ].ToLower() == "/i" )
                    && i + 1 < argv.Length )
                {
                    InputPath = argv[ i + 1 ];
                }
                if ( ( argv[ i ].ToLower() == "-o"
                    || argv[ i ].ToLower() == "/o" )
                    && i + 1 < argv.Length )
                {
                    OutputPath = argv[ i + 1 ];
                }
                if ( argv[ i ].ToLower() == "-u"
                    || argv[ i ].ToLower() == "/u" )
                {
                    DisableUnmusicPatch = true;
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
        private const int NEXTOR_BANKSIZE = 16 * 1024;

        private const int HB11_DISKBASIC_ENTRY_ADDRESS = 0x59DB;

        private const int NEXTOR_INIT2_ADDRESS = 0x47D8;
        private const int NEXTOR_INIT2_DEFAULT = 0x480C;

        private const int NEXTOR_BANK0_PATCH1_ADDRESS = 0x76F4;

        private const int NEXTOR_BANK0_PATCH2_ADDRESS = 0x76E0;
        private const int NEXTOR_BANK0_PATCH2_OFFSET_TABLE_LENGTH = 7;

        private const int NEXTOR_BANK3_PATCH4_BANK = 3;
        private const int NEXTOR_BANK3_PATCH4_ADDRESS = 0x76F7;
        private const int NEXTOR_BANK3_PATCH4_OFFSET_TABLE_LENGTH = 3;

        private const int NEXTOR_BANK4_PATCH3_BANK = 4;
        private const int NEXTOR_BANK4_PATCH3_ADDRESS = 0x7BD0;
        private const int NEXTOR_BANK4_PATCH3_OFFSET_TABLE_LENGTH = 11;

        private const int Z80_OPCODE_NOP = 0x00;
        private const int Z80_OPCODE_LDE = 0x1E;
        private const int Z80_OPCODE_LDHLI = 0x21;
        private const int Z80_OPCODE_ANDA = 0xA7;
        private const int Z80_OPCODE_XORA = 0xAF;
        private const int Z80_OPCODE_JP = 0xC3;
        private const int Z80_OPCODE_RET = 0xC9;
        private const int Z80_OPCODE_CALL = 0xCD;
        private const int Z80_OPCODE_POPHL = 0xE1;
        private const int Z80_OPCODE_PUSHHL = 0xE5;
        private const int Z80_OPCODE_DI = 0xF3;
        private const int Z80_OPCODE_ORI = 0xF6;

        private static byte[] NEXTOR_2_1_0_BETA1_PATTERN = new byte[] { 0xBE, 0x20, 0x13, 0x13, 0x23, 0xA7, 0x20, 0xF7 };
        private static byte[] NEXTOR_2_1_0_BETA1_PATCH = new byte[] { 0xC2, 0xED, 0x59, 0x13, 0x23, 0xA7, 0xC2, 0xD6, 0x59, 0XC3, 0xDF, 0x59 };

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

        private bool CheckPatchAreaEmpty( int address, int length, int bank = 0 )
        {
            bool chkempty = true;
            int chkcode = GetByte( address, bank );

            if ( chkcode != 0x00 && chkcode != 0xFF )
            {
                chkempty = false;
            }
            else
            {
                for ( int i = 1; i < length; i++ )
                {
                    if ( GetByte( address + i, bank ) != chkcode )
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

        private byte[] GetPatchBinary( string name )
        {
            var asm = Assembly.GetExecutingAssembly();
            using ( var stream = asm.GetManifestResourceStream( name ) )
            {
                var result = new byte[ stream?.Length ?? 0 ];
                stream?.Read( result, 0, result.Length );
                return result;
            }
        }

        private int AddressOf( byte[] pattern, int bank = 0 )
        {
            int banktopofs = bank * NEXTOR_BANKSIZE;
            int ofs = IndexOf( buf, pattern, banktopofs ) - banktopofs;

            if ( ofs < 0 || ofs >= NEXTOR_BANKSIZE )
            {
                return -1;
            }
            return ofs + NEXTOR_KERNEL_BASE_ADDRSS;
        }

        private void PatchByte( int addr, int value, int bank = 0 )
        {
            buf[ addr + bank * NEXTOR_BANKSIZE - NEXTOR_KERNEL_BASE_ADDRSS + 0 ] = ( byte )( value & 255 );
        }

        private void PatchWord( int addr, int value, int bank = 0 )
        {
            PatchByte( addr + 0, value >> 0, bank );
            PatchByte( addr + 1, value >> 8, bank );
        }

        private void PatchBytes( int dstbank, int dstaddr, byte[] src, int srcofs = 0, int len = -1 )
        {
            if ( len == -1 )
            {
                len = src.Length - srcofs;
            }
            Array.Copy( src, srcofs, buf, dstaddr + dstbank * NEXTOR_BANKSIZE - NEXTOR_KERNEL_BASE_ADDRSS, len );
        }

        private void PatchBytes( int dstaddr, byte[] src, int srcofs = 0, int len = -1 )
        {
            PatchBytes( 0, dstaddr, src, srcofs, len );
        }

        private int GetByte( int addr, int bank = 0 )
        {
            return buf[ addr + bank * NEXTOR_BANKSIZE - NEXTOR_KERNEL_BASE_ADDRSS + 0 ];
        }

        private int GetWord( int addr, int bank = 0 )
        {
            return GetByte( addr, bank ) | ( GetByte( addr + 1, bank ) << 8 );
        }

        private int[] GetOffsetTable( byte[] patch, int length )
        {
            var ofs = new int[ length ];

            for ( int i = 0; i < ofs.Length; i++ )
            {
                ofs[ i ] = patch[ i * 2 + 0 ] | ( patch[ i * 2 + 1 ] << 8 );
            }
            return ofs;
        }


        private bool Patch()
        {
            var HB11_DISKBASIC_ENTRY_CODE = GetPatchBinary( "hb11nex.hb11nex_search_diskbasic_entry_code.bin" );
            var HRUNC_HANDLING_CODE = GetPatchBinary( "hb11nex.hb11nex_search_h_runc_handling_code.bin" );
            var RTC_CODE = GetPatchBinary( "hb11nex.hb11nex_search_rtc_code.bin" );
            var HIMEM_CODE = GetPatchBinary( "hb11nex.hb11nex_search_himem_code.bin" );

            var bank0_patch = GetPatchBinary( "hb11nex.hb11nex_bank0.bin" );
            var bank0_ofs = GetOffsetTable( bank0_patch, NEXTOR_BANK0_PATCH2_OFFSET_TABLE_LENGTH );

            var bank3_patch = GetPatchBinary( "hb11nex.hb11nex_bank3.bin" );
            var bank3_ofs = GetOffsetTable( bank3_patch, NEXTOR_BANK3_PATCH4_OFFSET_TABLE_LENGTH );
            int bank3 = NEXTOR_BANK3_PATCH4_BANK;

            var bank4_patch = GetPatchBinary( "hb11nex.hb11nex_bank4.bin" );
            var bank4_ofs = GetOffsetTable( bank4_patch, NEXTOR_BANK4_PATCH3_OFFSET_TABLE_LENGTH );
            int bank4 = NEXTOR_BANK4_PATCH3_BANK;

            if ( !CheckPatchAreaEmpty( NEXTOR_BANK0_PATCH1_ADDRESS, NEXTOR_2_1_0_BETA1_PATCH.Length ) )
            {
                log.AppendLine( $"PATCH AREA{NEXTOR_BANK0_PATCH1_ADDRESS.ToString( "X4" )}:already used" );
                return false;
            }

            if ( !CheckPatchAreaEmpty( NEXTOR_BANK0_PATCH2_ADDRESS, bank0_patch.Length - 2 * bank0_ofs.Length ) )
            {
                log.AppendLine( $"PATCH AREA{NEXTOR_BANK0_PATCH2_ADDRESS.ToString( "X4" )}:already used" );
                return false;
            }

            if ( !CheckPatchAreaEmpty( NEXTOR_BANK4_PATCH3_ADDRESS, bank4_patch.Length - 2 * bank4_ofs.Length, bank4 ) )
            {
                log.AppendLine( $"PATCH AREA{NEXTOR_BANK4_PATCH3_ADDRESS.ToString( "X4" )}(bank{bank4}):already used" );
                return false;
            }

            int hb11entryaddress = AddressOf( HB11_DISKBASIC_ENTRY_CODE );

            if ( hb11entryaddress < 0 )
            {
                log.AppendLine( "DISK BASIC ENTRY CODE for HB-11:not found" );
                return false;
            }

            int init2handlingaddress = NEXTOR_INIT2_ADDRESS;
            if ( GetByte( init2handlingaddress - 2 ) != Z80_OPCODE_DI
                || GetByte( init2handlingaddress - 1 ) != Z80_OPCODE_LDHLI
                || GetByte( init2handlingaddress + 2 ) != Z80_OPCODE_PUSHHL
                || GetByte( init2handlingaddress + 3 ) != Z80_OPCODE_XORA )
            {
                log.AppendLine( "INIT2 HANDLING CODE:not found" );
                return false;
            }

            int init2handleraddress = GetWord( init2handlingaddress + 0 );
            if ( init2handleraddress != NEXTOR_INIT2_DEFAULT )
            {
                log.AppendLine( "INIT2 HANDLER CODE:unknown" );
                //return false;
            }

            int hrunchandlingaddress = AddressOf( HRUNC_HANDLING_CODE );

            if ( hrunchandlingaddress < 0 )
            {
                log.AppendLine( "H.RUNC HANDLING CODE:not found" );
                return false;
            }

            hrunchandlingaddress += HRUNC_HANDLING_CODE.Length;

            int hrunchandleraddress = GetWord( hrunchandlingaddress );

            int rtcaddress = AddressOf( RTC_CODE, bank4 );

            if ( rtcaddress < 0 )
            {
                log.AppendLine( "CLK_END:not found" );
                return false;
            }

            if ( AddressOf( NEXTOR_2_1_0_BETA1_PATTERN ) == HB11_DISKBASIC_ENTRY_ADDRESS - 4 )
            {
                // 2.1.0 beta1
                PatchBytes( NEXTOR_BANK0_PATCH1_ADDRESS, NEXTOR_2_1_0_BETA1_PATCH );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS - 3, Z80_OPCODE_JP );
                PatchWord( HB11_DISKBASIC_ENTRY_ADDRESS - 2, NEXTOR_BANK0_PATCH1_ADDRESS );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 0, Z80_OPCODE_JP );
                PatchWord( HB11_DISKBASIC_ENTRY_ADDRESS + 1, hb11entryaddress );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 3, Z80_OPCODE_NOP );

                log.AppendLine( "HB-11 PATCH MODE:Nextor 2.1.0 beta1" );
            }
            else if ( GetByte( HB11_DISKBASIC_ENTRY_ADDRESS - 4 ) == Z80_OPCODE_POPHL
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS - 3 ) == Z80_OPCODE_CALL
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS + 0 ) == Z80_OPCODE_CALL
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS + 3 ) == Z80_OPCODE_ANDA
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS + 4 ) == Z80_OPCODE_RET )
            {
                // 2.1.0 beta2
                // 2.1.0 rc1
                PatchBytes( NEXTOR_BANK0_PATCH1_ADDRESS, buf, HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3, 8 );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS - 3, Z80_OPCODE_JP );
                PatchWord( HB11_DISKBASIC_ENTRY_ADDRESS - 2, NEXTOR_BANK0_PATCH1_ADDRESS );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 0, Z80_OPCODE_JP );
                PatchWord( HB11_DISKBASIC_ENTRY_ADDRESS + 1, hb11entryaddress );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 3, Z80_OPCODE_NOP );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 4, Z80_OPCODE_NOP );

                log.AppendLine( "HB-11 PATCH MODE:Nextor 2.1.0 beta2" );
            }
            else if ( GetByte( HB11_DISKBASIC_ENTRY_ADDRESS - 3 ) == Z80_OPCODE_POPHL
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS - 2 ) == Z80_OPCODE_CALL
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS + 1 ) == Z80_OPCODE_CALL
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS + 4 ) == Z80_OPCODE_ANDA
                && GetByte( HB11_DISKBASIC_ENTRY_ADDRESS + 5 ) == Z80_OPCODE_RET )
            {
                // 2.1.0
                // 2.1.1 alpha1
                // 2.1.1 alpha2
                // 2.1.1 beta1
                // 2.1.1 beta2
                PatchBytes( NEXTOR_BANK0_PATCH1_ADDRESS, buf, HB11_DISKBASIC_ENTRY_ADDRESS - NEXTOR_KERNEL_BASE_ADDRSS - 3, 9 );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS - 3, Z80_OPCODE_JP );
                PatchWord( HB11_DISKBASIC_ENTRY_ADDRESS - 2, NEXTOR_BANK0_PATCH1_ADDRESS );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 0, Z80_OPCODE_JP );
                PatchWord( HB11_DISKBASIC_ENTRY_ADDRESS + 1, hb11entryaddress );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 3, Z80_OPCODE_NOP );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 4, Z80_OPCODE_NOP );
                PatchByte( HB11_DISKBASIC_ENTRY_ADDRESS + 5, Z80_OPCODE_NOP );

                log.AppendLine( "HB-11 PATCH MODE:Nextor 2.1.0" );
            }
            else
            {
                log.AppendLine( "HB-11 PATCH MODE:unknown kernel(bank0)" );
                return false;
            }

            PatchBytes( NEXTOR_BANK0_PATCH2_ADDRESS, bank0_patch, 2 * bank0_ofs.Length );

            PatchByte( NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 0 ], bank4 );

            PatchWord( init2handlingaddress, NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 1 ] );
            PatchWord( NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 2 ], NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 0 ] );

            PatchWord( hrunchandlingaddress, NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 3 ] );
            PatchWord( NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 4 ], NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 1 ] );

            PatchBytes( bank4, NEXTOR_BANK4_PATCH3_ADDRESS, bank4_patch, 2 * bank4_ofs.Length );

            int clk_end = rtcaddress;
            int clk_start = rtcaddress + 0x0b;
            int gt_date_time = rtcaddress + 0x16;
            int set_date = rtcaddress + 0x62;

            if ( GetByte( gt_date_time + 0, bank4 ) != Z80_OPCODE_CALL
                || GetWord( gt_date_time + 1, bank4 ) != clk_start
                || GetByte( gt_date_time + 3, bank4 ) != Z80_OPCODE_LDE
                || GetByte( gt_date_time + 4, bank4 ) != 0x0D
                || GetByte( set_date + 0, bank4 ) != Z80_OPCODE_CALL
                || GetWord( set_date + 1, bank4 ) != clk_start
                || GetByte( set_date + 3, bank4 ) != Z80_OPCODE_ORI
                || GetByte( set_date + 4, bank4 ) != 0x01 )
            {
                log.AppendLine( $"NORTC PATCH MODE:unknown kernel(bank{bank4})" );
                return false;
            }

            PatchWord( NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 2 ], init2handleraddress, bank4 );
            PatchWord( NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 3 ], hrunchandleraddress, bank4 );

            PatchWord( gt_date_time + 1, NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 4 ], bank4 );
            PatchWord( set_date + 1, NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 5 ], bank4 );

            PatchWord( NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 6 ], clk_start, bank4 );
            PatchWord( NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 7 ], clk_start, bank4 );
            PatchWord( NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 8 ], clk_end, bank4 );

            if ( !Parameters.DisableUnmusicPatch )
            {
                int dos2himem = AddressOf( HIMEM_CODE );
                if ( dos2himem < 0 )
                {
                    log.AppendLine( "DOS2HIMEM:not found" );
                    return false;
                }

                PatchByte( dos2himem + 0, Z80_OPCODE_CALL );
                PatchWord( dos2himem + 1, NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 5 ] );
                PatchWord( NEXTOR_BANK0_PATCH2_ADDRESS + bank0_ofs[ 6 ], NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 9 ] );

                if ( !CheckPatchAreaEmpty( NEXTOR_BANK3_PATCH4_ADDRESS, bank3_patch.Length - 2 * bank3_ofs.Length, bank3 ) )
                {
                    log.AppendLine( $"PATCH AREA{NEXTOR_BANK3_PATCH4_ADDRESS.ToString( "X4" )}(bank{bank3}):already used" );
                    return false;
                }

                int dos1himem = AddressOf( HIMEM_CODE, bank3 );
                if ( dos1himem < 0 )
                {
                    log.AppendLine( "DOS1HIMEM:not found" );
                    return false;
                }

                PatchBytes( bank3, NEXTOR_BANK3_PATCH4_ADDRESS, bank3_patch, 2 * bank3_ofs.Length );
                PatchByte( NEXTOR_BANK3_PATCH4_ADDRESS + bank3_ofs[ 0 ], bank4, bank3 );

                PatchByte( dos1himem + 0, Z80_OPCODE_CALL, bank3 );
                PatchWord( dos1himem + 1, NEXTOR_BANK3_PATCH4_ADDRESS + bank3_ofs[ 1 ], bank3 );
                PatchWord( NEXTOR_BANK3_PATCH4_ADDRESS + bank3_ofs[ 2 ], NEXTOR_BANK4_PATCH3_ADDRESS + bank4_ofs[ 10 ], bank3 );
            }

            // log.AppendLine( $"ADDRESS OF DISK BASIC ENTRY CODE:{hb11entryaddress.ToString( "X4" )}" );
            // log.AppendLine( $"ADDRESS OF H.RUNC HANDLING CODE:{( hrunchandlingaddress + HRUNC_HANDLING_CODE.Length ).ToString( "X4" )}" );
            // log.AppendLine( $"ADDRESS OF H.RUNC HANDLER CODE:{hrunchandleraddress.ToString( "X4" )}" );

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
            const string title = "NextorPatch For HB-11 version 0.0.5\n";

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
