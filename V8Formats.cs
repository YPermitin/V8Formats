using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DevelPlatform.SpecialTypes;

namespace DevelPlatform.OneCEUtils.V8Formats
{
    public class V8Formats
    {        
        public static readonly string V8P_VERSION = "1.0";
        public static readonly string V8P_RIGHT = "YPermitin (ypermitin@yandex.ru) www.develplatform.ru\n PSPlehanov (psplehanov@mail.ru)";

        public class V8File : IDisposable
        {
            #region global
            
            // Максимальный размер блока в памяти.
            // Используется если режим работы установлен в OPTIMAL.
            // В случае, если размер обрабатываемого блока превышает заданную величину,
            // то данные сохраняются во временный файл.
            // По умолчанию 1 МБ
            public const int MAX_BLOCK_SIZE_IN_MEMORY_BYTES = 1048576;
            public const int MAX_FILE_SIZE = 209715200;
            public static UInt32 V8_DEFAULT_PAGE_SIZE = 512;

            private static UInt32 V8_FF_SIGNATURE = 0x7fffffff;
            
            #endregion

            #region variables                     
                            
            public Mode OperationMode { set; get; }
            stFileHeader FileHeader;
            List<stElemAddr> ElemsAddrs;
            List<CV8Elem> Elems;
            bool IsDataPacked;

            private V8File _parentV8File;
            public V8File ParentV8File
            {
                get
                {
                    return _parentV8File;
                }
            }
            private Guid _objectId;
            public Guid ObjectId
            {
                get
                {
                    return this._objectId;
                }
            }
            private string _tmpFolder;
            public string tmpFolder
            {
                get
                {
                    if(string.IsNullOrEmpty(this._tmpFolder))
                    {
                        if (_parentV8File == null)                        
                            _tmpFolder = string.Format("{0}V8Formats{1}{2}", Path.GetTempPath(), Path.DirectorySeparatorChar, ObjectId);
                        else
                            _tmpFolder = string.Format("{0}{1}{2}", _parentV8File._tmpFolder, Path.DirectorySeparatorChar, ObjectId);

                        if (!Directory.Exists(_tmpFolder))
                            Directory.CreateDirectory(_tmpFolder);
                    }
                    return this._tmpFolder;
                }
            }
            
            #endregion

            #region structures

            struct stFileHeader
            {
                byte[] next_page_addr;
                byte[] page_size;
                byte[] storage_ver;
                byte[] reserved; // всегда 0x00000000 ?

                public static uint Size()
                {
                    return 4 + 4 + 4 + 4;
                }

                public byte[] ToBytes()
                {
                    byte[] resultBytes = new byte[16];

                    this.next_page_addr.CopyTo(resultBytes, 0);
                    this.page_size.CopyTo(resultBytes, 4);
                    this.storage_ver.CopyTo(resultBytes, 8);
                    this.reserved.CopyTo(resultBytes, 12);

                    return resultBytes;
                }

                public stFileHeader(byte[] pFileData, UInt32 indexBegin)
                {
                    this.next_page_addr = new byte[4];
                    for (int i = 0; i < 4; i++)
                        this.next_page_addr[i] = pFileData[i + indexBegin];

                    this.page_size = new byte[4];
                    for (int i = 0; i < 4; i++)
                        this.page_size[i] = pFileData[i + indexBegin + 4];

                    this.storage_ver = new byte[4];
                    for (int i = 0; i < 4; i++)
                        this.storage_ver[i] = pFileData[i + indexBegin + 8];

                    this.reserved = new byte[4];
                    for (int i = 0; i < 4; i++)
                        this.storage_ver[i] = pFileData[i + indexBegin + 12];
                }
            };

            public struct stElemAddr
            {
                public UInt32 elem_header_addr;
                public UInt32 elem_data_addr;
                public UInt32 fffffff; //всегда 0x7fffffff ?

                public static uint Size()
                {
                    return 4 + 4 + 4;
                }

                public byte[] ToBytes()
                {
                    byte[] byteResult = new byte[stElemAddr.Size()];

                    byte[] buf = BitConverter.GetBytes(elem_header_addr);
                    Array.Copy(buf, 0, byteResult, 0, 4);
                    buf = BitConverter.GetBytes(elem_data_addr);
                    Array.Copy(buf, 0, byteResult, 4, 4);
                    
                    buf = BitConverter.GetBytes(fffffff);
                    Array.Copy(buf, 0, byteResult, 8, 4);                    

                    return byteResult;
                }

                public stElemAddr(byte[] source, int beginIndex)
                {
                    this.elem_header_addr = BitConverter.ToUInt32(source, beginIndex);
                    this.elem_data_addr = BitConverter.ToUInt32(source, beginIndex + 4);
                    this.fffffff = BitConverter.ToUInt32(source, beginIndex + 8); ;
                }

                public stElemAddr(MemoryTributary source, int beginIndex)
                {
                    byte[] buffer;
                    BinaryReader binReader = new BinaryReader(source);
                    binReader.BaseStream.Position = beginIndex;
                    buffer = binReader.ReadBytes(12);

                    this.elem_header_addr = BitConverter.ToUInt32(buffer, 0);
                    this.elem_data_addr = BitConverter.ToUInt32(buffer, 4);
                    this.fffffff = BitConverter.ToUInt32(buffer, 8);
                }
            };

            struct stBlockHeader
            {
                public byte EOL_0D;
                public byte EOL_0A;
                public byte[] data_size_hex;
                public byte space1;
                public byte[] page_size_hex;
                public byte space2;
                public byte[] next_page_addr_hex;
                public byte space3;
                public byte EOL2_0D;
                public byte EOL2_0A;

                public static uint Size()
                {
                    return 1 + 1 + 8 + 1 + 8 + 1 + 8 + 1 + 1 + 1;
                }

                public byte[] ToBytes()
                {
                    byte[] resultBytes = new byte[stBlockHeader.Size()];

                    resultBytes[0] = this.EOL_0D;
                    resultBytes[1] = this.EOL_0A;
                    for (int i = 0; i < 8; i++)
                        resultBytes[i + 2] = this.data_size_hex[i];
                    resultBytes[10] = this.space1;
                    for (int i = 0; i < 8; i++)
                        resultBytes[i + 11] = this.page_size_hex[i];
                    resultBytes[19] = this.space2;
                    for (int i = 0; i < 8; i++)
                        resultBytes[i + 20] = this.next_page_addr_hex[i];
                    resultBytes[28] = this.space3;
                    resultBytes[29] = this.EOL2_0D;
                    resultBytes[30] = this.EOL2_0A;

                    return resultBytes;
                }

                public stBlockHeader(byte[] pFileData, UInt32 begIndex)
                {
                    this.EOL_0D = pFileData[0 + begIndex];
                    this.EOL_0A = pFileData[1 + begIndex];
                    // data_size_hex 
                    this.data_size_hex = new byte[8];
                    for (int i = 0; i < 8; i++)
                        this.data_size_hex[i] = pFileData[i + 2 + begIndex];
                    this.space1 = pFileData[10 + begIndex];
                    // page_size_hex 
                    this.page_size_hex = new byte[8];
                    for (int i = 0; i < 8; i++)
                        this.page_size_hex[i] = pFileData[i + 11 + begIndex];
                    this.space2 = pFileData[19 + begIndex];
                    // next_page_addr_hex 
                    this.next_page_addr_hex = new byte[8];
                    for (int i = 0; i < 8; i++)
                        this.next_page_addr_hex[i] = pFileData[i + 20 + begIndex];
                    this.space3 = pFileData[28 + begIndex];
                    this.EOL2_0D = pFileData[29 + begIndex];
                    this.EOL2_0A = pFileData[30 + begIndex];
                }
            };

            #endregion

            #region constructors

            public V8File()
            {
                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
                _objectId = Guid.NewGuid();
                OperationMode = Mode.Optimal;
            }
            public V8File(V8File ParentV8File)
            {
                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
                _objectId = Guid.NewGuid();
                OperationMode = ParentV8File.OperationMode;
                this._parentV8File = ParentV8File;
            }
            private V8File(BinaryReader pFileDataStream, int InflateSize, bool boolInflate = true, Mode OperationMode = Mode.Optimal)
            {
                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
                _objectId = Guid.NewGuid();
                this.OperationMode = OperationMode;                

                this.LoadFile(pFileDataStream, boolInflate);
            }
            private V8File(V8File ParentV8File, BinaryReader pFileDataStream, int InflateSize, bool boolInflate = true, Mode OperationMode = Mode.Optimal)
            {
                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
                _objectId = Guid.NewGuid();
                this._parentV8File = ParentV8File;
                this.OperationMode = OperationMode;

                this.LoadFile(pFileDataStream, boolInflate);
            }
            private V8File(MemoryTributary pFileDataStream, int InflateSize, bool boolInflate = true, Mode OperationMode = Mode.Optimal)
            {
                BinaryReader binReader = new BinaryReader(pFileDataStream);

                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
                _objectId = Guid.NewGuid();
                this.OperationMode = OperationMode;

                this.LoadFile(binReader, boolInflate);
            }
            private V8File(V8File ParentV8File, MemoryTributary pFileDataStream, int InflateSize, bool boolInflate = true, Mode OperationMode = Mode.Optimal)
            {
                BinaryReader binReader = new BinaryReader(pFileDataStream);

                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
                _objectId = Guid.NewGuid();
                this.OperationMode = OperationMode;
                this._parentV8File = ParentV8File;

                this.LoadFile(binReader, boolInflate);
            }

            #endregion

            #region InflareAndDeflate

            private bool Inflate(MemoryTributary compressedMemoryStream, out MemoryTributary outBufStream)
            {
                bool result = true;

                outBufStream = new MemoryTributary();

                try
                {
                    compressedMemoryStream.Position = 0;
                    System.IO.Compression.DeflateStream decompressStream = new System.IO.Compression.DeflateStream(compressedMemoryStream, System.IO.Compression.CompressionMode.Decompress);
                    decompressStream.CopyTo(outBufStream);                  
                }
                catch (Exception ex)
                {
                    outBufStream = compressedMemoryStream;
                    result = false;
                }

                return result;
            }

            private bool Deflate(MemoryTributary pDataStream, out MemoryTributary outBufStream)
            {
                bool result = true;

                int DataSize = (int)pDataStream.Length;
                outBufStream = new MemoryTributary();

                pDataStream.Position = 0;
                try
                {
                    MemoryTributary srcMemStream = pDataStream;
                    {
                        using (MemoryTributary compressedMemStream = new MemoryTributary())
                        {
                            using (System.IO.Compression.DeflateStream strmDef = new System.IO.Compression.DeflateStream(compressedMemStream, System.IO.Compression.CompressionMode.Compress))
                            {
                                srcMemStream.CopyTo(strmDef);
                            }

                            outBufStream = compressedMemStream;
                        }
                    }
                }
                catch (Exception ex)
                {
                    outBufStream = pDataStream;
                    result = false;
                }

                return result;
            }

            #endregion

            #region publicMethods

            public void Inflate(string in_filename, string out_filename, bool enableNewCode = true)
            {
                if (!File.Exists(in_filename))
                    throw new Exception("Input file not found!");

                using (FileStream fileReader = File.Open(in_filename, FileMode.Open))
                {
                    MemoryTributary memOutBuffer;
                    using (MemoryTributary memBuffer = new MemoryTributary())
                    {
                        fileReader.CopyTo(memBuffer);

                        bool success = Inflate(memBuffer, out memOutBuffer);
                        if (!success)
                            throw new Exception("Inflate error!");

                        using (FileStream fileWriter = new FileStream(out_filename, FileMode.Create))
                        {
                            memOutBuffer.Position = 0;
                            memOutBuffer.CopyTo(fileWriter);
                        }
                        memOutBuffer.Close();
                    }
                }
            }

            public void Deflate(string in_filename, string out_filename, bool enableNewCode = true)
            {
                if (!File.Exists(in_filename))
                    throw new Exception("Input file not found!");

                using (FileStream fileReader = File.Open(in_filename, FileMode.Open))
                {
                    MemoryTributary memOutBuffer;
                    using (MemoryTributary memBuffer = new MemoryTributary())
                    {
                        fileReader.CopyTo(memBuffer);

                        bool success = Deflate(memBuffer, out memOutBuffer);
                        if (!success)
                            throw new Exception("Deflate error!");

                        using (FileStream fileWriter = new FileStream(out_filename, FileMode.Create))
                        {
                            memOutBuffer.Position = 0;
                            memOutBuffer.CopyTo(fileWriter);
                        }
                        memOutBuffer.Close();
                    }
                }
            }

            public void UnpackToFolder(string filenameIn, string dirName, string UnpackElemWithName = null, bool printProgress = false, bool enableNewCode = true)
            {

                FileInfo filePath = new FileInfo(filenameIn);
                if (!filePath.Exists)
                {
                    throw new Exception("UnpackToFolder. Input file not found!");
                }

                try
                {
                    System.IO.DirectoryInfo dirUnpack = new DirectoryInfo(dirName);
                    dirUnpack.Create();
                }
                catch
                {
                    throw new Exception("UnpackToFolder. Error in creating directory!");
                }

                using (BinaryReader reader = new BinaryReader(File.Open(filenameIn, FileMode.Open)))
                {
                    LoadFile(reader, false);
                }

                string filename_out = dirName;
                filename_out += "/FileHeader";
                
                try
                {
                    System.IO.File.WriteAllBytes(filename_out, this.FileHeader.ToBytes());
                }
                catch
                {
                    Console.WriteLine("UnpackToFolder. Error in creating file!");
                }

                int ElemsNum = Elems.Count();

                foreach (CV8Elem elem in this.Elems)
                {
                    // если передано имя блока для распаковки, пропускаем все остальные
                    if (!string.IsNullOrEmpty(UnpackElemWithName) && (UnpackElemWithName != elem.elemName))
                        continue;

                    filename_out = dirName;
                    filename_out += "/";
                    filename_out += elem.elemName;
                    filename_out += ".header";

                    using (FileStream writer = File.Open(filename_out, FileMode.OpenOrCreate))
                    {
                        MemoryTributary memBuffer = elem.GetHeaderLikeMemStream();
                        memBuffer.CopyTo(writer);
                    }

                    filename_out = dirName;
                    filename_out += "/";
                    filename_out += elem.elemName;
                    filename_out += ".data";

                    using (FileStream writer = File.Open(filename_out, FileMode.OpenOrCreate))
                    {
                        MemoryTributary memBuffer = elem.GetDataLikeMemStream();
                        memBuffer.CopyTo(writer);
                    }
                }

                ClearTempData();
            }

            public void PackFromFolder(string dirname, string outFileName, bool enableNewCode = true)
            {
                string filename;

                filename = string.Format("{0}\\FileHeader", dirname);
                byte[] fileHeaderBytes = File.ReadAllBytes(filename);
                this.FileHeader = new stFileHeader(fileHeaderBytes, 0);

                string[] files = Directory.GetFiles(dirname);
                var filesWitoutFileHeader = files.Where(el => !el.EndsWith("FileHeader"));
                var filesBlockHeaders = filesWitoutFileHeader.Where(el => el.EndsWith(".header"));

                UInt32 ElemsNum = 0;
                ElemsNum = (UInt32)filesWitoutFileHeader.Count();

                Elems.Clear();
                ElemsAddrs.Clear();

                foreach (string file in filesBlockHeaders)
                {
                    CV8Elem elem = new CV8Elem(this);

                    filename = file;
                    using (FileStream fileReader = File.Open(filename, FileMode.Open))
                    {
                        MemoryTributary memBuffer = new MemoryTributary();
                        fileReader.CopyTo(memBuffer);
                        elem.SetHeaderFromMemStream(memBuffer);
                        elem.HeaderSize = (UInt32)memBuffer.Length;
                    }

                    filename = filename.Replace(".header", ".Data");
                    using (FileStream fileReader = File.Open(filename, FileMode.Open))
                    {
                        MemoryTributary memBuffer = new MemoryTributary();
                        fileReader.CopyTo(memBuffer);
                        elem.SetDataFromMemStream(memBuffer);
                        elem.DataSize = (UInt32)memBuffer.Length;
                    }

                    Elems.Add(elem);
                }

                SaveFile(outFileName, true);

                ClearTempData();
            }

            public void Parse(string filename, string dirname, int level = 0, bool enableNewCode = true)
            {
                FileInfo filePath = new FileInfo(filename);
                if (!filePath.Exists)                
                    throw new Exception("Parse. Input file not found!");

                using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    bool isV8File = IsV8File(reader);
                    if (!isV8File)
                        throw new Exception("Input file is not 1C:Enterprise 8.x format!");

                    LoadFile(reader);

                    SaveFileToFolder(dirname, reader);
                }

                ClearTempData();
            }

            public void Build(string dirName, string filename, int level = 0, bool enableNewCode = true)
            {
                LoadFileFromFolder(dirName, true);

                Pack(true);

                SaveFile(filename, true);

                ClearTempData();
            }

            #endregion

            #region privateMethods

            private static bool IsV8File(MemoryTributary inputFileStream)
            {
                BinaryReader binReader = new BinaryReader(inputFileStream);
                return IsV8File(binReader);
            }
            private static bool IsV8File(BinaryReader inputFileStream)
            {
                if (inputFileStream.BaseStream.Length == 0)
                {
                    return false;
                }

                // проверим чтобы длина файла не была меньше длины заголовка файла и заголовка блока адресов
                if (inputFileStream.BaseStream.Length < stFileHeader.Size() + stBlockHeader.Size())
                    return false;

                long prevPosition = inputFileStream.BaseStream.Position;
                inputFileStream.BaseStream.Position = 0;

                stBlockHeader pBlockHeader = new stBlockHeader(inputFileStream.ReadBytes((int)stBlockHeader.Size() + 16), 16);

                inputFileStream.BaseStream.Position = prevPosition;

                if (pBlockHeader.EOL_0D != 0x0d ||
                        pBlockHeader.EOL_0A != 0x0a ||
                        pBlockHeader.space1 != 0x20 ||
                        pBlockHeader.space2 != 0x20 ||
                        pBlockHeader.space3 != 0x20 ||
                        pBlockHeader.EOL2_0D != 0x0d ||
                        pBlockHeader.EOL2_0A != 0x0a)
                {

                    return false;
                }

                return true;
            }

            private void LoadFile(BinaryReader inputFileStream, bool boolInflate = true, bool UnpackWhenNeed = true)
            {
                long prevPosition = inputFileStream.BaseStream.Position;
                inputFileStream.BaseStream.Position = 0;

                bool useTempFiles = false;
                if(OperationMode == Mode.FileSystem)                
                    useTempFiles = true;
                else if (OperationMode == Mode.Optimal)
                {
                    // В оптимальном режиме, если обрабатываемый файл больше 200 МБ,
                    // то автоматически включается режим использования файловой системы
                    if (inputFileStream.BaseStream.Length > MAX_FILE_SIZE)
                        OperationMode = Mode.FileSystem;
                }

                MemoryTributary InflateBufferStream;
                UInt32 InflateSize = 0;

                this.FileHeader = new stFileHeader(inputFileStream.ReadBytes((int)stFileHeader.Size()), 0);

                stBlockHeader pBlockHeader = new stBlockHeader(inputFileStream.ReadBytes((int)stBlockHeader.Size()), 0);

                UInt32 ElemsAddrsSize;
                MemoryTributary pElemsAddrsStream;
                ReadBlockData(inputFileStream, pBlockHeader, stFileHeader.Size(), out pElemsAddrsStream, out ElemsAddrsSize);

                UInt32 ElemsNum = ElemsAddrsSize / stElemAddr.Size();

                Elems.Clear();
                ElemsAddrs.Clear();

                for (UInt32 i = 0; i < ElemsNum; i++)
                {

                    stElemAddr pElemsAddrs = new stElemAddr(pElemsAddrsStream, (int)(i * stElemAddr.Size()));
                    ElemsAddrs.Add(pElemsAddrs);

                    if (pElemsAddrs.fffffff != V8_FF_SIGNATURE)
                    {
                        ElemsNum = i;
                        break;
                    }

                    inputFileStream.BaseStream.Position = pElemsAddrs.elem_header_addr;
                    pBlockHeader = new stBlockHeader(inputFileStream.ReadBytes((int)stBlockHeader.Size()), 0);

                    if (pBlockHeader.EOL_0D != 0x0d ||
                        pBlockHeader.EOL_0A != 0x0a ||
                        pBlockHeader.space1 != 0x20 ||
                        pBlockHeader.space2 != 0x20 ||
                        pBlockHeader.space3 != 0x20 ||
                        pBlockHeader.EOL2_0D != 0x0d ||
                        pBlockHeader.EOL2_0A != 0x0a)
                    {
                        throw new Exception("Header is not correct!");
                    }

                    UInt32 ElemsAddrsSizeHeader = 0;
                    MemoryTributary pHeaderStream;
                    UInt32 DataSize = 0;
                    MemoryTributary pDataStream;
                    ReadBlockData(inputFileStream, pBlockHeader, pElemsAddrs.elem_header_addr, out pHeaderStream, out ElemsAddrsSizeHeader);

                    //080228 Блока данных может не быть, тогда адрес блока данных равен 0x7fffffff
                    if (pElemsAddrs.elem_data_addr != V8_FF_SIGNATURE)
                    {
                        inputFileStream.BaseStream.Position = pElemsAddrs.elem_data_addr;
                        pBlockHeader = new stBlockHeader(inputFileStream.ReadBytes((int)stBlockHeader.Size()), 0);
                        ReadBlockData(inputFileStream, pBlockHeader, pElemsAddrs.elem_data_addr, out pDataStream, out DataSize);
                    }
                    else
                    {
                        throw new Exception("Incorrect data block!");
                    }

                    CV8Elem elem = new CV8Elem(pHeaderStream, ElemsAddrsSizeHeader, pDataStream, (UInt32)pDataStream.Length, this, false, false, useTempFiles);

                    if (boolInflate && IsDataPacked)
                    {
                        bool success = Inflate(elem.GetDataLikeMemStream(), out InflateBufferStream);

                        if (!success)
                        {
                            IsDataPacked = false;
                            elem.SetDataFromMemStream(InflateBufferStream);
                            elem.DataSize = (UInt32)InflateBufferStream.Length;
                            elem.IsV8File = false;
                        }
                        else
                        {
                            elem.NeedUnpack = false; // отложенная распаковка не нужна
                            elem.pData = null; //нераспакованные данные больше не нужны
                            if (IsV8File(InflateBufferStream))
                            {
                                elem.UnpackedData = new V8File(this, InflateBufferStream, (int)InflateSize, boolInflate, this.OperationMode);
                                elem.pData = null;
                                elem.IsV8File = true;
                            }
                            else
                            {
                                elem.SetDataFromMemStream(InflateBufferStream);
                                elem.DataSize = InflateSize;
                                elem.IsV8File = false;
                            }
                        }
                    }

                    elem.InitElemName(inputFileStream, pElemsAddrs);
                    Elems.Add(elem);

                }

                inputFileStream.BaseStream.Position = prevPosition;
            }
            private void LoadFileFromFolder(string dirname, bool enableNewCode = true)
            {
                long sourceDIrectorySize = DirSize(new DirectoryInfo(dirname));
                if (sourceDIrectorySize > V8File.MAX_FILE_SIZE)
                    this.OperationMode = Mode.FileSystem;

                byte[] headerBytes = new byte[] {0xFF, 0xFF, 0xFF, 0x7F,
                                                 0x0, 0x2, 0x0, 0x0,
                                                 0x0, 0x0, 0x0, 0x0,
                                                 0x0, 0x0, 0x0, 0x0};

                FileHeader = new stFileHeader(headerBytes, 0);

                this.Elems.Clear();
                this.ElemsAddrs.Clear();

                string[] srcFiles = Directory.GetFiles(dirname, "*");

                foreach (string srcFile in srcFiles)
                {
                    FileInfo srcFileInfo = new FileInfo(srcFile);
                    if (srcFileInfo.Name[0] == '.')
                        continue;

                    using (BinaryReader reader = new BinaryReader(File.Open(srcFile, FileMode.Open)))
                    {
                        CV8Elem elem = new CV8Elem(this);

                        elem.IsV8File = false;

                        elem.HeaderSize = V8File.CV8Elem.stElemHeaderBegin.Size() + (UInt32)srcFileInfo.Name.Length * 2 + 4;
                        elem.SetElemName(srcFileInfo.Name, srcFileInfo.Name.Length);
                                                
                        elem.DataSize = (UInt32)reader.BaseStream.Length;
                        MemoryTributary buferData = new MemoryTributary();
                        reader.BaseStream.CopyTo(buferData);
                        elem.SetDataFromMemStream(buferData);

                        this.Elems.Add(elem);
                    }
                }

                string[] srcDirectories = Directory.GetDirectories(dirname, "*");

                foreach (string srcDir in srcDirectories)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(srcDir);
                    if (dirInfo.Name[0] == '.')
                        continue;

                    CV8Elem elem = new CV8Elem(this);

                    elem.IsV8File = true;

                    elem.HeaderSize = V8File.CV8Elem.stElemHeaderBegin.Size() + (UInt32)dirInfo.Name.Length * 2 + 4;
                    elem.SetElemName(dirInfo.Name, dirInfo.Name.Length);
                                        
                    elem.UnpackedData = new V8File(this);
                    elem.UnpackedData.LoadFileFromFolder(srcDir);

                    this.Elems.Add(elem);
                }
            }

            private bool SaveFileToFolder(string dirName, BinaryReader inputFileStream)
            {
                bool success = true;
                try
                {
                    System.IO.DirectoryInfo dirUnpack = new DirectoryInfo(dirName);
                    dirUnpack.Create();
                }
                catch
                {
                    throw new Exception("UnpackToFolder. Error in creating directory!");
                }

                string filename_out;

                UInt32 ElemsNum = (UInt32)this.Elems.Count;

                foreach (CV8Elem elem in this.Elems)
                {
                    int ElemNum = this.Elems.IndexOf(elem);

                    filename_out = string.Format("{0}\\{1}", dirName, elem.elemName);

                    if (!elem.IsV8File)
                    {
                        try
                        {
                            System.IO.File.WriteAllBytes(filename_out, elem.pData);
                        }
                        catch
                        {
                            throw new Exception("UnpackToFolder. Error in creating file!");
                        }
                    }
                    else
                    {
                        success = elem.UnpackedData.SaveFileToFolder(filename_out, inputFileStream);
                        if (!success)
                            break;
                    }
                }

                return success;
            }
            private void SaveFile(string filename, bool enableNewCode = true)
            {
                using (FileStream strWriter = new FileStream(filename, System.IO.FileMode.Create))
                {
                    UInt32 stElemSize = stElemAddr.Size();
                    UInt32 ElemsNum = (UInt32)Elems.Count;

                    UInt32 cur_block_addr = stFileHeader.Size() + stBlockHeader.Size();
                    if (stElemAddr.Size() * ElemsNum < V8_DEFAULT_PAGE_SIZE)
                        cur_block_addr += V8_DEFAULT_PAGE_SIZE; // 512 - стандартный размер страницы 0x200
                    else
                        cur_block_addr += stElemAddr.Size() * ElemsNum;

                    byte[] pTempElemsAddrs = new byte[Elems.Count * stElemSize];
                    foreach (CV8Elem elem in Elems)
                    {
                        int elIndex = Elems.IndexOf(elem);
                        stElemAddr curAddr = new stElemAddr();

                        curAddr.elem_header_addr = cur_block_addr;
                        cur_block_addr += stBlockHeader.Size() + elem.HeaderSize;

                        curAddr.elem_data_addr = cur_block_addr;
                        cur_block_addr += stBlockHeader.Size();

                        if (elem.DataSize > V8_DEFAULT_PAGE_SIZE)
                            cur_block_addr += elem.DataSize;
                        else
                            cur_block_addr += V8_DEFAULT_PAGE_SIZE;

                        curAddr.fffffff = 0x7fffffff;

                        byte[] tmpAddrBytes = curAddr.ToBytes();
                        Array.Copy(tmpAddrBytes, 0, pTempElemsAddrs, elIndex * stElemSize, stElemSize);
                    }

                    UInt32 cur_pos = 0;

                    // записываем заголовок
                    strWriter.Write(FileHeader.ToBytes(), 0, (int)stFileHeader.Size());
                    cur_pos += stFileHeader.Size();

                    // записываем адреса элементов
                    byte[] buffer;
                    if (pTempElemsAddrs.Length < V8_DEFAULT_PAGE_SIZE)
                    {
                        buffer = new byte[V8_DEFAULT_PAGE_SIZE + stBlockHeader.Size()];
                    }
                    else
                    {
                        buffer = new byte[pTempElemsAddrs.Length + stBlockHeader.Size()];
                    }

                    UInt32 bufCurPos = 0;
                    SaveBlockDataToBuffer(ref buffer, ref bufCurPos, pTempElemsAddrs);
                    strWriter.Write(buffer, 0, buffer.Length);
                    cur_pos += bufCurPos;

                    // записываем элементы (заголовок и данные)
                    foreach (CV8Elem elem in Elems)
                    {
                        using (MemoryTributary memBuffer = new MemoryTributary())
                        {
                            BinaryWriter binMemBuffer = new BinaryWriter(memBuffer);

                            memBuffer.Position = 0;
                            SaveBlockDataToBuffer(ref binMemBuffer, elem.GetHeaderLikeMemStream(), elem.HeaderSize);

                            // Переносим данные из memBuffer в файл
                            binMemBuffer.BaseStream.Position = 0;
                            for (int i = 0; i < binMemBuffer.BaseStream.Length; i++)
                            {
                                strWriter.WriteByte(Convert.ToByte(binMemBuffer.BaseStream.ReadByte()));
                            }
                        }

                        using (MemoryTributary memBuffer = new MemoryTributary())
                        {
                            BinaryWriter binMemBuffer = new BinaryWriter(memBuffer);

                            memBuffer.Position = 0;
                            bufCurPos = 0;

                            SaveBlockDataToBuffer(ref binMemBuffer, elem.GetDataLikeMemStream());
                            cur_pos += bufCurPos;

                            // Переносим данные из memBuffer в файл
                            binMemBuffer.BaseStream.Position = 0;
                            for (int i = 0; i < binMemBuffer.BaseStream.Length; i++)
                            {
                                strWriter.WriteByte(Convert.ToByte(binMemBuffer.BaseStream.ReadByte()));
                            }
                        }
                    }
                }
            }
            
            private void Pack(bool enableNewCode = true)
            {
                MemoryTributary DeflateBufferStream = new MemoryTributary();
                MemoryTributary DataBufferStream = new MemoryTributary();

                foreach (CV8Elem elem in Elems)
                {
                    UInt32 elemNum = (UInt32)Elems.IndexOf(elem);

                    if (!elem.IsV8File)
                    {
                        DataBufferStream = new MemoryTributary();
                        bool success = Deflate(elem.GetDataLikeMemStream(), out DataBufferStream);
                        if (!success)
                            throw new Exception("Ошибка сжатия данных. Некорректный формат данных!");

                        elem.SetDataFromMemStream(DataBufferStream);
                        elem.DataSize = (UInt32)DataBufferStream.Length;
                    }
                    else
                    {
                        DataBufferStream = new MemoryTributary();
                        MemoryTributary outBufSteram = new MemoryTributary();
                        elem.UnpackedData.GetData(out DataBufferStream);
                        bool success = Deflate(DataBufferStream, out outBufSteram);
                        if (!success)
                            throw new Exception("Ошибка сжатия данных. Некорректный формат данных!");

                        elem.UnpackedData = new V8File(this);
                        elem.IsV8File = false;

                        elem.SetDataFromMemStream(outBufSteram);
                        elem.DataSize = (UInt32)outBufSteram.Length;
                    }
                }

                DeflateBufferStream.Close();
                DataBufferStream.Close();
            }
            private void GetData(out MemoryTributary dataStream)
            {
                UInt32 stElemSize = stElemAddr.Size();
                UInt32 NeedDataBufferSize = 0;
                NeedDataBufferSize += stFileHeader.Size();

                // заголовок блока и данные блока - адреса элементов с учетом минимальной страницы 512 байт
                NeedDataBufferSize += stBlockHeader.Size() + (UInt32)Math.Max(stElemSize * Elems.Count, V8_DEFAULT_PAGE_SIZE);

                foreach (CV8Elem elem in Elems)
                {
                    // заголовок блока и данные блока - заголовок элемента
                    NeedDataBufferSize += stBlockHeader.Size() + elem.HeaderSize;

                    // заголовок блока и данные блока - данные элемента с учетом минимальной страницы 512 байт
                    NeedDataBufferSize += stBlockHeader.Size() + (UInt32)Math.Max(elem.DataSize, V8_DEFAULT_PAGE_SIZE);
                }

                // Создаем и заполняем данные по адресам элементов
                byte[] pTempElemsAddrs = new byte[Elems.Count * stElemSize];
                UInt32 cur_block_addr = stFileHeader.Size() + stBlockHeader.Size();
                if (stElemSize * Elems.Count < V8_DEFAULT_PAGE_SIZE)
                    cur_block_addr += V8_DEFAULT_PAGE_SIZE; // 512 - стандартный размер страницы 0x200
                else
                    cur_block_addr += stElemSize * (UInt32)Elems.Count;

                foreach (CV8Elem elem in Elems)
                {
                    UInt32 elNum = (UInt32)Elems.IndexOf(elem);

                    stElemAddr tmpAdrr = new stElemAddr();

                    tmpAdrr.elem_header_addr = cur_block_addr;
                    cur_block_addr += stBlockHeader.Size() + elem.HeaderSize;

                    tmpAdrr.elem_data_addr = cur_block_addr;
                    cur_block_addr += stBlockHeader.Size();

                    if (elem.DataSize > V8_DEFAULT_PAGE_SIZE)
                        cur_block_addr += elem.DataSize;
                    else
                        cur_block_addr += V8_DEFAULT_PAGE_SIZE;

                    tmpAdrr.fffffff = 0x7fffffff;

                    byte[] tmpAddrBytes = tmpAdrr.ToBytes();
                    Array.Copy(tmpAddrBytes, 0, pTempElemsAddrs, elNum * stElemSize, stElemSize);
                }

                dataStream = new MemoryTributary();
                dataStream.SetLength(NeedDataBufferSize);
                BinaryWriter dataStreamBin = new BinaryWriter(dataStream);
                //DataBuffer = new byte[NeedDataBufferSize];
                UInt32 cur_pos = 0;

                // записываем заголовок
                byte[] fileHeader = FileHeader.ToBytes();

                dataStreamBin.Write(fileHeader);
                //Array.Copy(fileHeader, 0, DataBuffer, cur_pos, fileHeader.Length);
                cur_pos += stFileHeader.Size();

                // записываем адреса элементов
                SaveBlockDataToBuffer(ref dataStreamBin, ref cur_pos, pTempElemsAddrs);

                // записываем элементы (заголовок и данные)
                foreach (CV8Elem elem in Elems)
                {
                    SaveBlockDataToBuffer(ref dataStreamBin, ref cur_pos, elem.pHeader, elem.HeaderSize);
                    SaveBlockDataToBuffer(ref dataStreamBin, ref cur_pos, elem.pData);
                }

                pTempElemsAddrs = null;
            }
            private void SaveBlockDataToBuffer(ref BinaryWriter DataBufferStream, ref UInt32 cur_pos, byte[] pBlockData, UInt32 PageSize = 512)
            {
                UInt32 BlockDataSize = (UInt32)pBlockData.Length;

                if (PageSize < BlockDataSize)
                    PageSize = BlockDataSize;

                stBlockHeader CurBlockHeader = new stBlockHeader();

                CurBlockHeader.EOL_0D = 0xd;
                CurBlockHeader.EOL_0A = 0xa;
                CurBlockHeader.EOL2_0D = 0xd;
                CurBlockHeader.EOL2_0A = 0xa;

                CurBlockHeader.data_size_hex = _intTo_BytesChar(BlockDataSize);
                CurBlockHeader.page_size_hex = _intTo_BytesChar(PageSize);
                CurBlockHeader.next_page_addr_hex = _intTo_BytesChar(0x7fffffff);

                CurBlockHeader.space1 = 0x20;
                CurBlockHeader.space2 = 0x20;
                CurBlockHeader.space3 = 0x20;

                DataBufferStream.BaseStream.Position = cur_pos;
                DataBufferStream.Write(CurBlockHeader.ToBytes());
                //Array.Copy(CurBlockHeader.ToBytes(), 0, DataBuffer, cur_pos, stBlockHeader.Size());

                cur_pos += stBlockHeader.Size();

                //Array.Copy(pBlockData, 0, DataBuffer, cur_pos, BlockDataSize);
                DataBufferStream.BaseStream.Position = cur_pos;
                DataBufferStream.Write(pBlockData);
                cur_pos += BlockDataSize;

                for (UInt32 i = 0; i < PageSize - BlockDataSize; i++)
                {
                    DataBufferStream.BaseStream.Position = cur_pos;
                    DataBufferStream.Write(0);
                    //    DataBuffer[cur_pos] = 0;
                    cur_pos++;
                }
            }
            private void SaveBlockDataToBuffer(ref BinaryWriter DataBufferStream, MemoryTributary pBlockDataStream, UInt32 PageSize = 512)
            {
                UInt32 BlockDataSize = (UInt32)pBlockDataStream.Length;

                if (PageSize < BlockDataSize)
                    PageSize = BlockDataSize;

                stBlockHeader CurBlockHeader = new stBlockHeader();

                CurBlockHeader.EOL_0D = 0xd;
                CurBlockHeader.EOL_0A = 0xa;
                CurBlockHeader.EOL2_0D = 0xd;
                CurBlockHeader.EOL2_0A = 0xa;

                CurBlockHeader.data_size_hex = _intTo_BytesChar(BlockDataSize);
                CurBlockHeader.page_size_hex = _intTo_BytesChar(PageSize);
                CurBlockHeader.next_page_addr_hex = _intTo_BytesChar(0x7fffffff);

                CurBlockHeader.space1 = 0x20;
                CurBlockHeader.space2 = 0x20;
                CurBlockHeader.space3 = 0x20;

                DataBufferStream.BaseStream.Position = 0;
                DataBufferStream.Write(CurBlockHeader.ToBytes());

                pBlockDataStream.Position = 0;
                for (int i = 0; i < pBlockDataStream.Length; i++)
                {
                    DataBufferStream.Write(Convert.ToByte(pBlockDataStream.ReadByte()));
                }

                DataBufferStream.BaseStream.SetLength(DataBufferStream.BaseStream.Length + (PageSize - BlockDataSize));
            }
            private void SaveBlockDataToBuffer(ref byte[] DataBuffer, ref UInt32 cur_pos, byte[] pBlockData, UInt32 PageSize = 512)
            {
                UInt32 BlockDataSize = (UInt32)pBlockData.Length;

                if (PageSize < BlockDataSize)
                    PageSize = BlockDataSize;

                stBlockHeader CurBlockHeader = new stBlockHeader();

                CurBlockHeader.EOL_0D = 0xd;
                CurBlockHeader.EOL_0A = 0xa;
                CurBlockHeader.EOL2_0D = 0xd;
                CurBlockHeader.EOL2_0A = 0xa;

                CurBlockHeader.data_size_hex = _intTo_BytesChar(BlockDataSize);
                CurBlockHeader.page_size_hex = _intTo_BytesChar(PageSize);
                CurBlockHeader.next_page_addr_hex = _intTo_BytesChar(0x7fffffff);

                CurBlockHeader.space1 = 0x20;
                CurBlockHeader.space2 = 0x20;
                CurBlockHeader.space3 = 0x20;

                Array.Copy(CurBlockHeader.ToBytes(), 0, DataBuffer, cur_pos, stBlockHeader.Size());

                cur_pos += stBlockHeader.Size();

                Array.Copy(pBlockData, 0, DataBuffer, cur_pos, BlockDataSize);
                cur_pos += BlockDataSize;

                for (UInt32 i = 0; i < PageSize - BlockDataSize; i++)
                {
                    DataBuffer[cur_pos] = 0;
                    cur_pos++;
                }
            }
            private void ReadBlockData(BinaryReader inputFileStream, stBlockHeader? pBlockHeader, UInt32 elemHeaderAddr, out MemoryTributary pBlockDataStream, out UInt32 BlockDataSize)
            {
                pBlockDataStream = new MemoryTributary();

                if (pBlockHeader == null)
                    pBlockHeader = new stBlockHeader();

                UInt32 data_size = 0, page_size = 0, next_page_addr = 0;
                UInt32 read_in_bytes = 0, bytes_to_read = 0;

                BlockDataSize = 0;

                if (pBlockHeader != null)
                {
                    data_size = _httoi(((stBlockHeader)pBlockHeader).data_size_hex);
                    if (data_size == 0)
                    {
                        throw new Exception("ReadBlockData. Block мData == NULL.");
                    }
                }

                read_in_bytes = 0;
                UInt32 adr = elemHeaderAddr + stBlockHeader.Size(); // Конец header
                while (read_in_bytes < data_size)
                {
                    page_size = _httoi(((stBlockHeader)pBlockHeader).page_size_hex);
                    next_page_addr = _httoi(((stBlockHeader)pBlockHeader).next_page_addr_hex);

                    bytes_to_read = Math.Min(page_size, data_size - read_in_bytes);

                    inputFileStream.BaseStream.Position = adr;
                    pBlockDataStream.Write(inputFileStream.ReadBytes((int)bytes_to_read), 0, (int)bytes_to_read);
                    read_in_bytes += bytes_to_read;

                    if (next_page_addr != V8Formats.V8File.V8_FF_SIGNATURE) // есть следующая страница
                    {
                        adr = next_page_addr + stBlockHeader.Size();
                        inputFileStream.BaseStream.Position = next_page_addr;
                        pBlockHeader = new stBlockHeader(inputFileStream.ReadBytes((int)stBlockHeader.Size()), 0);
                    }
                    else
                        break;
                }

                BlockDataSize = data_size;
            }
             
            #endregion

            #region Service

            private UInt32 _httoi(byte[] value)
            {
                UInt32 result = 0;
                
                string newByte = System.Text.Encoding.Default.GetString(value);
                result = UInt32.Parse(newByte, System.Globalization.NumberStyles.HexNumber);                         

                return result;
            }
            private byte[] _intTo_BytesChar(UInt32 value)
            {
                string valueString = IntToHexString((int)value, 8).ToLower();
                byte[] resultBytes = new byte[8];

                for (int i = 0; i < valueString.Length; i++)
                    resultBytes[i] = Convert.ToByte(valueString[i]);

                return resultBytes;
            }
            private byte[] _inttobytes(UInt32 value)
            {
                string valueString = IntToHexString((int)value, 8).ToUpper();

                byte[] resultBytes = new byte[8];

                for (int i = 0; i < valueString.Length; i++)
                {
                    switch(valueString[i])
                    {
                        case 'A':                            
                            resultBytes[i] = 10;
                            break;                            
                        case 'B':
                            resultBytes[i] = 11;
                            break;
                        case 'C':
                            resultBytes[i] = 12;
                            break;
                        case 'D':
                            resultBytes[i] = 13;
                            break;
                        case 'E':
                            resultBytes[i] = 14;
                            break;
                        case 'F':
                            resultBytes[i] = 15;
                            break;
                        default:
                            resultBytes[i] = (byte)(Convert.ToByte(valueString[i]) - 0x30);
                            break;
                    }
                }

                return resultBytes;
            }
            private static String IntToHexString(int n, int len)
            {
                char[] ch = new char[len--];
                for (int i = len; i >= 0; i--)
                {
                    ch[len - i] = ByteToHexChar((byte)((uint)(n >> 4 * i) & 15));
                }
                return new String(ch);
            }
            public static char ByteToHexChar(byte b)
            {
                if (b < 0 || b > 15)
                    throw new Exception("IntToHexChar: input out of range for Hex value");
                return b < 10 ? (char)(b + 48) : (char)(b + 55);
            }

            private void ClearTempData()
            {
                if(!string.IsNullOrEmpty(tmpFolder))
                {
                    try
                    {
                        Directory.Delete(_tmpFolder, true);
                    }
                    catch {
                    }
                }

                string V8FormatsTmp = string.Format("{0}V8Formats{1}", Path.GetTempPath(), Path.DirectorySeparatorChar);
                if(Directory.Exists(V8FormatsTmp))
                {
                    string[] foundDirectories = Directory.GetDirectories(V8FormatsTmp);
                    foreach (string dirFullname in foundDirectories)
                    {
                        try
                        {
                            DirectoryInfo tmpDir = new DirectoryInfo(dirFullname);
                            if (tmpDir.CreationTime < DateTime.Now.AddHours(-1))
                            {
                                tmpDir.Delete(true);
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
            private static long DirSize(DirectoryInfo d)
            {
                long Size = 0;
                // Add file sizes.
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    Size += fi.Length;
                }
                // Add subdirectory sizes.
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    Size += DirSize(di);
                }
                return (Size);
            }
            #endregion

            #region specialData

            private class CV8Elem
            {
                #region constructors

                public CV8Elem()
                {
                    this.IsV8File = false;
                    this.HeaderSize = 0;
                    this.DataSize = 0;                    
                    this._objectId = Guid.NewGuid();

                    if (this.UnpackedData != null)
                    {
                        if (this.UnpackedData.OperationMode == Mode.FileSystem)
                            this.useTempFiles = true;
                        else if (this.UnpackedData.OperationMode == Mode.MemoryUsage)
                            this.useTempFiles = false;
                        else if (this.UnpackedData.OperationMode == Mode.Optimal)
                        {
                            if (this.DataSize > V8File.MAX_BLOCK_SIZE_IN_MEMORY_BYTES)
                                this.useTempFiles = true;
                            else
                                this.useTempFiles = false;
                        } else
                            this.useTempFiles = false;
                    } else
                        this.useTempFiles = false;
                }
                public CV8Elem(V8File UnpackedData)
                {
                    this.UnpackedData = UnpackedData;
                    this.IsV8File = false;
                    this.HeaderSize = 0;
                    this.DataSize = 0;
                    this._objectId = Guid.NewGuid();

                    if (this.UnpackedData != null)
                    {
                        if (this.UnpackedData.OperationMode == Mode.FileSystem)
                            this.useTempFiles = true;
                        else if (this.UnpackedData.OperationMode == Mode.MemoryUsage)
                            this.useTempFiles = false;
                        else if (this.UnpackedData.OperationMode == Mode.Optimal)
                        {
                            if (this.DataSize > V8File.MAX_BLOCK_SIZE_IN_MEMORY_BYTES)
                                this.useTempFiles = true;
                            else
                                this.useTempFiles = false;
                        }
                        else
                            this.useTempFiles = false;
                    }
                    else
                        this.useTempFiles = false;
                }
                public CV8Elem(byte[] pHeader, UInt32 HeaderSize, byte[] pData, UInt32 DataSize, V8File UnpackedData, bool IsV8File, bool NeedUnpack, bool useTempFiles = false)
                {
                    this.UnpackedData = UnpackedData;                                                        
                    this.IsV8File = IsV8File;
                    this.NeedUnpack = NeedUnpack;                   
                    this._objectId = Guid.NewGuid();

                    if (this.UnpackedData != null)
                    {
                        if (this.UnpackedData.OperationMode == Mode.FileSystem)
                            this.useTempFiles = true;
                        else if (this.UnpackedData.OperationMode == Mode.MemoryUsage)
                            this.useTempFiles = false;
                        else if (this.UnpackedData.OperationMode == Mode.Optimal)
                        {
                            if (this.DataSize > V8File.MAX_BLOCK_SIZE_IN_MEMORY_BYTES)
                                this.useTempFiles = true;
                            else
                                this.useTempFiles = false;
                        }
                        else
                            this.useTempFiles = false;
                    } else
                        this.useTempFiles = false;

                    this.pHeader = pHeader;
                    this.HeaderSize = HeaderSize;
                    this.pData = pData;
                    this.DataSize = DataSize;
                }
                public CV8Elem(MemoryTributary pHeader, UInt32 HeaderSize, MemoryTributary pData, UInt32 DataSize, V8File UnpackedData, bool IsV8File, bool NeedUnpack, bool useTempFiles = false)
                {
                    this.UnpackedData = UnpackedData;
                    this.IsV8File = IsV8File;
                    this.NeedUnpack = NeedUnpack;
                    this._objectId = Guid.NewGuid();

                    if (this.UnpackedData != null)
                    {
                        if (this.UnpackedData.OperationMode == Mode.FileSystem)
                            this.useTempFiles = true;
                        else if (this.UnpackedData.OperationMode == Mode.MemoryUsage)
                            this.useTempFiles = false;
                        else if (this.UnpackedData.OperationMode == Mode.Optimal)
                        {
                            if (this.DataSize > V8File.MAX_BLOCK_SIZE_IN_MEMORY_BYTES)
                                this.useTempFiles = true;
                            else
                                this.useTempFiles = false;
                        }
                        else
                            this.useTempFiles = false;
                    } else
                        this.useTempFiles = false;

                    if (!this.useTempFiles)
                        this.pHeader = pHeader.ToArray();
                    else
                    {
                        this.SetHeaderFromMemStream(pHeader);
                    }
                    this.HeaderSize = HeaderSize;

                    if(!this.useTempFiles)
                        this.pData = pData.ToArray();
                    else
                    {
                        this.SetDataFromMemStream(pData);
                    }
                    this.DataSize = DataSize;
                }

                #endregion

                #region structures

                public struct stElemHeaderBegin
                {
                    public static uint Size()
                    {
                        return 8 + 8 + 4;
                    }
                }

                #endregion

                #region variables

                private Guid _objectId;
                public Guid ObjectId
                {
                    get
                    {
                        return this._objectId;
                    }
                }

                public string elemName;
                public UInt32 elemNameLen;

                private byte[] _pHeader; // TODO: Утечка памяти
                public byte[] pHeader
                {
                    set
                    {       
                        // При использовании временных файлов генерируем путь для сохранения данных
                        // в противном случае сохраняем данные непосредственно в памяти     
                        if(useTempFiles && value != null)
                        {
                            GenerateTempFileName("header");                            
                            File.WriteAllBytes(tmpHeaderFile, value);
                        } else
                            this._pHeader = value;
                    }

                    get
                    {
                        // При использовании временных файлов получаем данные из файловой системы
                        // в противном случае получаем их непосредственно из памяти     
                        if (useTempFiles)
                        {
                            return File.ReadAllBytes(tmpHeaderFile);
                        } else
                            return this._pHeader;
                    }
                }
                public UInt32 HeaderSize;
                private byte[] _pData; // TODO: Утечка памяти
                public byte[] pData
                {
                    set
                    {
                        // При использовании временных файлов генерируем путь для сохранения данных
                        // в противном случае сохраняем данные непосредственно в памяти     
                        if (useTempFiles && value != null)
                        {
                            GenerateTempFileName("data");                            
                            File.WriteAllBytes(tmpDataFile, value);
                        }
                        else
                            this._pData = value;
                    }

                    get
                    {
                        // При использовании временных файлов получаем данные из файловой системы
                        // в противном случае получаем их непосредственно из памяти     
                        if (useTempFiles)
                        {
                            return File.ReadAllBytes(tmpDataFile);
                        }
                        else
                            return this._pData;
                    }
                }

                public UInt32 DataSize;
                public V8File UnpackedData;
                public bool IsV8File;
                public bool NeedUnpack;

                private bool useTempFiles;
                private string tmpHeaderFile;
                private string tmpDataFile;

                #endregion

                #region methods

                public void InitElemName(byte[] pFileData, stElemAddr ElemAddr)
                {
                    this.elemNameLen = (ElemAddr.elem_data_addr - 4 - ElemAddr.elem_header_addr - stBlockHeader.Size() - stElemHeaderBegin.Size()) / 2;

                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    char[] ElemName = new char[0];
                    char[] ElemNameBuf = new char[this.elemNameLen];

                    int validChars = 0;
                    for (UInt32 j = 0; j < this.elemNameLen * 2; j += 2)
                    {
                        char curChar = Convert.ToChar(pFileData[j + ElemAddr.elem_header_addr + stBlockHeader.Size() + stElemHeaderBegin.Size()]);

                        if (invalidChars.Where(ch => ch == curChar).Count() != 0)
                        {
                            ElemName = new char[validChars];
                            break;
                        }

                        ElemNameBuf[j / 2] = curChar;
                        validChars++;
                    }

                    if (ElemName.Length != 0)
                    {
                        for (int i = 0; i < ElemName.Length; i++)
                            ElemName[i] = ElemNameBuf[i];
                    }
                    else
                    {
                        ElemName = ElemNameBuf;
                    }

                    this.elemName = new string(ElemName);
                }
                public void InitElemName(BinaryReader inputFileStream, stElemAddr ElemAddr)
                {
                    this.elemNameLen = (ElemAddr.elem_data_addr - 4 - ElemAddr.elem_header_addr - stBlockHeader.Size() - stElemHeaderBegin.Size()) / 2;

                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    char[] ElemName = new char[0];
                    char[] ElemNameBuf = new char[this.elemNameLen];

                    int validChars = 0;
                    for (UInt32 j = 0; j < this.elemNameLen * 2; j += 2)
                    {
                        inputFileStream.BaseStream.Position = j + ElemAddr.elem_header_addr + stBlockHeader.Size() + stElemHeaderBegin.Size();
                        char curChar = Convert.ToChar(inputFileStream.ReadByte());

                        if (invalidChars.Where(ch => ch == curChar).Count() != 0)
                        {
                            ElemName = new char[validChars];
                            break;
                        }

                        ElemNameBuf[j / 2] = curChar;
                        validChars++;
                    }

                    if (ElemName.Length != 0)
                    {
                        for (int i = 0; i < ElemName.Length; i++)
                            ElemName[i] = ElemNameBuf[i];
                    }
                    else
                    {
                        ElemName = ElemNameBuf;
                    }

                    this.elemName = new string(ElemName);
                }
                public void SetElemName(string ElemName, int ElemNameLen)
                {
                    byte[] pHeaderBuffer = new byte[this.HeaderSize];

                    UInt32 stElemHeaderBeginSize = V8File.CV8Elem.stElemHeaderBegin.Size();

                    for (int j = 0; j < ElemNameLen * 2; j += 2, stElemHeaderBeginSize += 2)
                    {
                        pHeaderBuffer[stElemHeaderBeginSize] = Convert.ToByte(ElemName[j / 2]);
                        pHeaderBuffer[stElemHeaderBeginSize + 1] = 0;
                    }

                    MemoryTributary bufferHeader = new MemoryTributary(pHeaderBuffer);
                    this.SetHeaderFromMemStream(bufferHeader);
                }

                public void SetDataFromMemStream(MemoryTributary source)
                {
                    if (useTempFiles)
                    {
                        GenerateTempFileName("data");
                        using (FileStream file = new FileStream(tmpDataFile, FileMode.Create, FileAccess.Write))
                        {
                            source.WriteTo(file);
                        }
                    } else
                    {
                        this.pData = source.ToArray();
                    }
                }
                public void SetHeaderFromMemStream(MemoryTributary source)
                {
                    if (useTempFiles)
                    {
                        GenerateTempFileName("header");
                        using (FileStream file = new FileStream(tmpHeaderFile, FileMode.Create, FileAccess.Write))
                        {
                            source.WriteTo(file);
                        }
                    } else
                    {
                        this.pHeader = source.ToArray();
                    }
                }
                public MemoryTributary GetDataLikeMemStream()
                {
                    MemoryTributary memStream = new MemoryTributary();
                    if (useTempFiles)
                    {
                        using (FileStream tmpFileStream = new FileStream(tmpDataFile, FileMode.Open))
                        {
                            tmpFileStream.CopyTo(memStream);
                        }
                    } else
                    {
                        if (this._pData.Length > 0)
                            memStream.Write(this._pData, 0, (int)this.DataSize);
                    }
                    memStream.Position = 0;
                    return memStream;
                }
                public MemoryTributary GetHeaderLikeMemStream()
                {
                    MemoryTributary memStream = new MemoryTributary();
                    if (useTempFiles)
                    {
                        using (FileStream tmpFileStream = new FileStream(tmpHeaderFile, FileMode.Open))
                        {
                            tmpFileStream.CopyTo(memStream);
                        }
                    } else
                    {
                        if(this._pHeader.Length > 0)
                            memStream.Write(this._pHeader, 0, (int)this.HeaderSize);                        
                    }

                    memStream.Position = 0;
                    return memStream;
                }

                private void GenerateTempFileName(string prefix)
                {                    
                    string generatedFileName = string.Format("{0}{1}{2}.{3}", UnpackedData.tmpFolder, Path.DirectorySeparatorChar, ObjectId, prefix);
                    if (prefix == "header")
                        tmpHeaderFile = generatedFileName;
                    else if(prefix == "data")
                        tmpDataFile = generatedFileName;
                }

                #endregion
            }

            public enum Mode
            {
                Optimal,
                MemoryUsage,
                FileSystem
            }

            #endregion

            #region IDisposable Support
            private bool disposedValue = false; // Для определения избыточных вызовов

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: освободить управляемое состояние (управляемые объекты).
                        this.Elems.Clear();
                        this.ElemsAddrs.Clear();                        
                        this._parentV8File = null;                        
                    }

                    // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                    // TODO: задать большим полям значение NULL.

                    disposedValue = true;
                }
            }

            // TODO: переопределить метод завершения, только если Dispose(bool disposing) выше включает код для освобождения неуправляемых ресурсов.
            ~V8File()
            {
                ClearTempData();
                // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
                Dispose(false);
            }

            // Этот код добавлен для правильной реализации шаблона высвобождаемого класса.
            public void Dispose()
            {
                // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
                Dispose(true);
                // TODO: раскомментировать следующую строку, если метод завершения переопределен выше.
                GC.SuppressFinalize(this);
            }
            #endregion
        }

        public class GRS
        {
            // Класс находится в разработке...
        }

        public class MXL
        {
            // Класс находится в разработке...
        }
    }
}
