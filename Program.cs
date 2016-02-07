using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DevelPlatform.OneCEUtils.V8Formats;
using System.IO;

namespace DevelPlatform
{
    class V8UnpackProgram
    {
        static void usage()
        {
            StringBuilder message = new StringBuilder();
            message.AppendFormat("V8Formats Version {0} Copyright (c)\n {1}", V8Formats.V8P_VERSION, V8Formats.V8P_RIGHT);
            message.AppendLine();
            message.AppendLine();
            message.AppendLine("Unpack, pack, deflate and inflate 1C v8 file (*.cf),(*.epf),(*.erf)");
            message.AppendLine();
            message.AppendLine("V8UNPACK");
            message.AppendLine();            
            message.AppendLine("  -U[NPACK]     in_filename.cf     out_dirname");
            message.AppendLine("  -PA[CK]       in_dirname         out_filename.cf");
            message.AppendLine("  -I[NFLATE]    in_filename.data   out_filename");
            message.AppendLine("  -D[EFLATE]    in_filename        filename.data");
            message.AppendLine("  -E[XAMPLE]");
            message.AppendLine("  -BAT");
            message.AppendLine("  -P[ARSE]      in_filename        out_dirname");
            message.AppendLine("  -B[UILD]      in_dirname         out_filename");
            message.AppendLine("  -V[ERSION]");

            Console.WriteLine(message.ToString());
        }

        static void version()
        {
            Console.WriteLine(V8Formats.V8P_VERSION);
        }

        static void Main(string[] args)
        {
            string[] argsNew = new string[5];
            for (int i = 0; i < 5; i++)
            {
                if (args.Length > i)
                    argsNew[i] = args[i];
                else
                    argsNew[i] = string.Empty;
            }
            args = argsNew;

            string cur_mode = string.Empty;
            if (args.Length > 1)
                cur_mode = args[0];

            cur_mode = cur_mode.ToLower();

            if (cur_mode == "-version" || cur_mode == "-v") {
                    version();
            } else if (cur_mode == "-inflate" || cur_mode == "-i" || cur_mode == "-und" || cur_mode == "-undeflate") {

                V8Formats.V8File V8File = new V8Formats.V8File();
                V8File.Inflate(args[1], args[2]);
                return;
            } else if (cur_mode == "-deflate" || cur_mode == "-d") {
                V8Formats.V8File V8File = new V8Formats.V8File();
                V8File.Deflate(args[1], args[2]);
            } else if (cur_mode == "-unpack" || cur_mode == "-u" || cur_mode == "-unp") {

                V8Formats.V8File V8File = new V8Formats.V8File();
                V8File.UnpackToFolder(args[1], args[2], null, true);
            } else if (cur_mode == "-pack" || cur_mode == "-pa") {

                V8Formats.V8File V8File = new V8Formats.V8File();
                V8File.PackFromFolder(args[1], args[2]);
            } else if (cur_mode == "-parse" || cur_mode == "-p") {
                V8Formats.V8File V8File = new V8Formats.V8File();
                V8File.Parse(args[1], args[2]);
            } else if (cur_mode == "-build" || cur_mode == "-b") {
                V8Formats.V8File V8File = new V8Formats.V8File();
                int ret = V8File.Build(args[1], args[2]);
                if (ret == V8Formats.V8File.SHOW_USAGE)
                    usage();
            } else if (cur_mode == "-bat") {
                StringBuilder message = new StringBuilder();
                message.AppendLine("if %1 == P GOTO PACK");
                message.AppendLine("if %1 == p GOTO PACK");
                message.AppendLine();
                message.AppendLine();
                message.AppendLine(":UNPACK");
                message.AppendLine("V8Unpack.exe -unpack      %2                              %2.unp");
                message.AppendLine("V8Unpack.exe -undeflate   %2.unp\\metadata.data            %2.unp\\metadata.data.und");
                message.AppendLine("V8Unpack.exe -unpack      %2.unp\\metadata.data.und        %2.unp\\metadata.unp");
                message.AppendLine("GOTO END");
                message.AppendLine();
                message.AppendLine();
                message.AppendLine(":PACK");
                message.AppendLine("V8Unpack.exe -pack        %2.unp\\metadata.unp            %2.unp\\metadata_new.data.und");
                message.AppendLine("V8Unpack.exe -deflate     %2.unp\\metadata_new.data.und   %2.unp\\metadata.data");
                message.AppendLine("V8Unpack.exe -pack        %2.unp                         %2.new.cf");
                message.AppendLine();
                message.AppendLine();
                message.AppendLine(":END");

                Console.WriteLine(message.ToString());
            } else if (cur_mode == "-example" || cur_mode == "-e") {
                StringBuilder message = new StringBuilder();
                message.AppendLine();
                message.AppendLine();
                message.AppendLine("UNPACK");
                message.AppendLine("V8Unpack.exe -unpack      1Cv8.cf                          1Cv8.unp" );
                message.AppendLine("V8Unpack.exe -undeflate   1Cv8.unp\\metadata.data          1Cv8.unp\\metadata.data.und");
                message.AppendLine("V8Unpack.exe -unpack      1Cv8.unp\\metadata.data.und      1Cv8.unp\\metadata.unp");
                message.AppendLine();
                message.AppendLine();
                message.AppendLine("PACK");
                message.AppendLine("V8Unpack.exe -pack        1Cv8.unp\\metadata.unp           1Cv8.unp\\metadata_new.data.und");
                message.AppendLine("V8Unpack.exe -deflate     1Cv8.unp\\metadata_new.data.und  1Cv8.unp\\metadata.data");
                message.AppendLine("V8Unpack.exe -pack        1Cv8.und                         1Cv8_new.cf");
                message.AppendLine();
                message.AppendLine();

                Console.WriteLine(message.ToString());
            } else
                usage();

            Console.WriteLine("Press any key for exit...");
            Console.ReadKey();
        }
    }
}
