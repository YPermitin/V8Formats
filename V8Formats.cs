using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DevelPlatform.Utils;

namespace DevelPlatform.OneCEUtils.V8Formats
{
    public class V8Formats
    {        
        public static readonly string V8P_VERSION = "1.0";
        public static readonly string V8P_RIGHT = "YPermitin (ypermitin@yandex.ru) www.develplatform.ru\n PSPlehanov (psplehanov@mail.ru)";

        public class CONF_ERF_EPF
        {
            #region variables
            public static readonly int V8UNPACK_ERROR = -50;
            public static readonly int V8UNPACK_NOT_V8_FILE = V8UNPACK_ERROR - 1;
            public static readonly int V8UNPACK_HEADER_ELEM_NOT_CORRECT = V8UNPACK_ERROR - 2;

            public static readonly int V8UNPACK_INFLATE_ERROR = V8UNPACK_ERROR - 20;
            public static readonly int V8UNPACK_INFLATE_IN_FILE_NOT_FOUND = V8UNPACK_INFLATE_ERROR - 1;

            public static readonly int V8UNPACK_DEFLATE_ERROR = V8UNPACK_ERROR - 30;
            public static readonly int V8UNPACK_DEFLATE_IN_FILE_NOT_FOUND = V8UNPACK_ERROR - 1;

            public static readonly int SHOW_USAGE = -22;
            public static readonly UInt32 CHUNK = 16384;

            public static UInt32 V8_DEFAULT_PAGE_SIZE = 512;
            public static UInt32 V8_FF_SIGNATURE = 0x7fffffff;
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

            stFileHeader FileHeader;
            List<stElemAddr> ElemsAddrs;
            List<CV8Elem> Elems;
            bool IsDataPacked;

            public CONF_ERF_EPF()
            {
                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();
            }

            public CONF_ERF_EPF(byte[] pFileData, int InflateSize, bool boolInflate = true)
            {
                this.IsDataPacked = true;
                Elems = new List<CV8Elem>();
                ElemsAddrs = new List<stElemAddr>();

                this.LoadFile(pFileData, InflateSize, boolInflate);
            }

            int Inflate(byte[] pData, out byte[] outBuf, UInt32 DataSize, out UInt32 out_buf_len)
            {                
                int ret = 0;

                out_buf_len = DataSize + CHUNK;
                outBuf = new byte[(int)CHUNK];

                try
                {
                    using (MemoryStream memStreamExtract = new MemoryStream())
                    {
                        using (MemoryStream memStream = new MemoryStream(pData))
                        {
                            using (System.IO.Compression.DeflateStream strmDef = new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionMode.Decompress))
                            {
                                strmDef.CopyTo(memStreamExtract);
                            }

                        }
                        outBuf = memStreamExtract.ToArray();
                        out_buf_len = (UInt32)outBuf.Length;
                    }
                }
                catch
                {
                    outBuf = pData;
                    out_buf_len = DataSize;
                    ret = -1;
                }

                return ret;
            }

            public int Inflate(string in_filename, string out_filename)
            {
                int ret;

                if (!File.Exists(in_filename))
                    return V8UNPACK_INFLATE_IN_FILE_NOT_FOUND;

                byte[] in_file = File.ReadAllBytes(in_filename);

                byte[] out_file_result;
                UInt32 out_file_size;

                ret = Inflate(in_file, out out_file_result, (UInt32)in_file.Length, out out_file_size);

                if (ret != 0)
                    return V8UNPACK_INFLATE_ERROR;

                File.WriteAllBytes(out_filename, out_file_result);

                return 0;
            }

            public int Deflate(string in_filename, string out_filename)
            {
                int ret;

                if (!File.Exists(in_filename))
                    return V8UNPACK_DEFLATE_IN_FILE_NOT_FOUND;

                byte[] in_file = File.ReadAllBytes(in_filename);
                byte[] out_file_result;

                ret = Deflate(in_file, out out_file_result);
                
                if (ret != 0)
                    return V8UNPACK_DEFLATE_ERROR;

                File.WriteAllBytes(out_filename, out_file_result);

                return 0;
            }

            int Deflate(byte[] pData, out byte[] outBuf)
            {
                int ret = 0;

                int DataSize = pData.Length;
                outBuf = new byte[(int)CHUNK];

                try
                {
                    using (MemoryStream srcMemStream = new MemoryStream(pData))
                    {
                        using (MemoryStream compressedMemStream = new MemoryStream())
                        {
                            using (System.IO.Compression.DeflateStream strmDef = new System.IO.Compression.DeflateStream(compressedMemStream, System.IO.Compression.CompressionMode.Compress))
                            {
                                srcMemStream.CopyTo(strmDef);
                            }

                            outBuf = compressedMemStream.ToArray();
                        }                        
                    }
                }
                catch
                {
                    outBuf = pData;
                    ret = -1;
                }

                return ret;
            }

            int SaveFile(string filename)
            {             
                using(FileStream strWriter = new FileStream(filename, System.IO.FileMode.Create))
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
                    } else
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
                        buffer = new byte[elem.HeaderSize + stBlockHeader.Size()];
                        bufCurPos = 0;
                        SaveBlockDataToBuffer(ref buffer, ref bufCurPos, elem.pHeader, elem.HeaderSize);                        
                        strWriter.Write(buffer, 0, buffer.Length);
                        cur_pos += bufCurPos;

                        if (elem.DataSize < V8_DEFAULT_PAGE_SIZE)
                        {
                            buffer = new byte[V8_DEFAULT_PAGE_SIZE + stBlockHeader.Size()];
                        }
                        else
                        {
                            buffer = new byte[elem.DataSize + stBlockHeader.Size()];
                        } 
                        
                        bufCurPos = 0;
                        SaveBlockDataToBuffer(ref buffer, ref bufCurPos, elem.pData);                        
                        strWriter.Write(buffer, 0, buffer.Length);
                        cur_pos += bufCurPos;
                    }
                }

                return 0;
            }

            byte[] GetBinaryOfFile()
            {
                using (MemoryStream strWriter = new MemoryStream())
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
                        buffer = new byte[elem.HeaderSize + stBlockHeader.Size()];
                        bufCurPos = 0;
                        SaveBlockDataToBuffer(ref buffer, ref bufCurPos, elem.pHeader, elem.HeaderSize);
                        strWriter.Write(buffer, 0, buffer.Length);
                        cur_pos += bufCurPos;

                        if (elem.DataSize < V8_DEFAULT_PAGE_SIZE)
                        {
                            buffer = new byte[V8_DEFAULT_PAGE_SIZE + stBlockHeader.Size()];
                        }
                        else
                        {
                            buffer = new byte[elem.DataSize + stBlockHeader.Size()];
                        }

                        bufCurPos = 0;
                        SaveBlockDataToBuffer(ref buffer, ref bufCurPos, elem.pData);
                        strWriter.Write(buffer, 0, buffer.Length);
                        cur_pos += bufCurPos;
                    }

                    return strWriter.ToArray();
                }                
            }
            int Pack()
            {
                byte[] DeflateBuffer;
                byte[] DataBuffer; 
	
                int ret = 0;

                bool printProgress = true;
                int onePercent = Elems.Count / 50;
                if (printProgress && onePercent != 0)
                {
                    Console.Write("Progress (50 points): ");
                }

                foreach (CV8Elem elem in Elems)
                {
                    UInt32 elemNum = (UInt32)Elems.IndexOf(elem);
                    if (printProgress && Elems.Count > 0 && onePercent > 0 && elemNum % onePercent == 0)
                    {
                        if (elemNum % (onePercent * 10) == 0)
                            Console.Write("|");
                        else
                            Console.Write(".");
                    }

                    if (!elem.IsV8File)
                    {
                        ret = Deflate(elem.pData, out DataBuffer);
                        if (ret != 0)
                            return ret;

                        elem.pData = DataBuffer;
                        elem.DataSize = (UInt32)DataBuffer.Length;
                    }
                    else
                    {
                        elem.UnpackedData.GetData(out DataBuffer);
                        ret = Deflate(DataBuffer, out DeflateBuffer);
                        if (ret != 0)
                            return ret;

                        elem.UnpackedData = new CONF_ERF_EPF();
                        elem.IsV8File = false;
                        elem.pData = DeflateBuffer;
                        elem.DataSize = (UInt32)DeflateBuffer.Length;
                    }
                }

                if (printProgress && onePercent != 0)
                {
                    Console.WriteLine();
                }

                DataBuffer = null;
                DeflateBuffer = null;

                return 0;
            }

            void GetData(out byte[] DataBuffer)
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

                DataBuffer = new byte[NeedDataBufferSize];
                UInt32 cur_pos = 0;

                // записываем заголовок
                byte[] fileHeader = FileHeader.ToBytes();                
                Array.Copy(fileHeader, 0, DataBuffer, cur_pos, fileHeader.Length);
                cur_pos += stFileHeader.Size();

                // записываем адреса элементов
                SaveBlockDataToBuffer(ref DataBuffer, ref cur_pos, pTempElemsAddrs);

                // записываем элементы (заголовок и данные)
                foreach (CV8Elem elem in Elems)
	            {
                    SaveBlockDataToBuffer(ref DataBuffer, ref cur_pos, elem.pHeader, elem.HeaderSize);
                    SaveBlockDataToBuffer(ref DataBuffer, ref cur_pos, elem.pData);
                }

                pTempElemsAddrs = null;
            }

            int SaveBlockDataToBuffer(ref byte[] DataBuffer, ref UInt32 cur_pos, byte[] pBlockData, UInt32 PageSize = 512)
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
                CurBlockHeader.next_page_addr_hex  = _intTo_BytesChar(0x7fffffff);

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

                return 0;
            }

            int SetElemName(CV8Elem Elem, string ElemName, int ElemNameLen)
            {
                UInt32 stElemHeaderBeginSize = CONF_ERF_EPF.CV8Elem.stElemHeaderBegin.Size();

                for (int j = 0; j <ElemNameLen * 2; j+=2, stElemHeaderBeginSize+=2)
                {
                    Elem.pHeader[stElemHeaderBeginSize] = Convert.ToByte(ElemName[j/2]);
                    Elem.pHeader[stElemHeaderBeginSize + 1] = 0;
                }

                return 0;
            }

            public int Build(string dirName, string filename, int level = 0)
            {
                LoadFileFromFolder(dirName);
                Console.WriteLine("LoadFileFromFolder: ok\n");

	            Pack();
	            Console.WriteLine("Pack: ok\n");
                
	            SaveFile(filename);

	            return 0;
            }

            public void Build(string dirName, out byte[] file, int level = 0)
            {
                LoadFileFromFolder(dirName);
                Console.WriteLine("LoadFileFromFolder: ok\n");

                Pack();
                Console.WriteLine("Pack: ok\n");

                file = GetBinaryOfFile();
            }

            int LoadFileFromFolder(string dirname)
            {             
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
                    byte[] srcFileData = File.ReadAllBytes(srcFileInfo.FullName);

                    if (srcFileInfo.Name[0] == '.')
                        continue;
                    
                    CV8Elem elem = new CV8Elem();

                    elem.HeaderSize = CONF_ERF_EPF.CV8Elem.stElemHeaderBegin.Size() + (UInt32)srcFileInfo.Name.Length * 2 + 4;
                    elem.pHeader = new byte[elem.HeaderSize];                  

                    SetElemName(elem, srcFileInfo.Name, srcFileInfo.Name.Length);

                    elem.IsV8File = false;
                    elem.DataSize = (UInt32)srcFileData.Length;
                    elem.pData = srcFileData;

                    this.Elems.Add(elem);
                }

                string[] srcDirectories = Directory.GetDirectories(dirname, "*");

                foreach (string srcDir in srcDirectories)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(srcDir);

                    if (dirInfo.Name[0] == '.')
                        continue;
                    
                    CV8Elem elem = new CV8Elem();

                    elem.HeaderSize = CONF_ERF_EPF.CV8Elem.stElemHeaderBegin.Size() + (UInt32)dirInfo.Name.Length * 2 + 4;
                    elem.pHeader = new byte[elem.HeaderSize];

                    SetElemName(elem, dirInfo.Name, dirInfo.Name.Length);

                    elem.IsV8File = true;
                    elem.UnpackedData = new CONF_ERF_EPF();
                    elem.UnpackedData.LoadFileFromFolder(srcDir);

                    this.Elems.Add(elem);
                }   

                return 0;
            }

            public int UnpackToFolder(string filenameIn, string dirName, string UnpackElemWithName = null, bool printProgress = false)
            {
                byte[] pFileData = null;

                int ret = 0;
                int FileDataSize = 0;

                FileInfo filePath = new FileInfo(filenameIn);
                if (!filePath.Exists)
                {
                    Console.WriteLine("UnpackToFolder. Input file not found!");
                    return -1;
                }

                using (FileStream fileInStream = new FileStream(filePath.FullName, FileMode.Open))
                {
                    using (BinaryReader fileIn = new BinaryReader(fileInStream))
                    {
                        FileDataSize = (int)filePath.Length;
                                                
                        byte[] sz_r = fileIn.ReadBytes(FileDataSize);
                        if (sz_r.Length != FileDataSize)
                        {
                            Console.WriteLine("UnpackToFolder. Error in reading file!");
                            return sz_r.Length;
                        }
                        pFileData = sz_r;
                        if (pFileData.Length == 0)
                        {
                            Console.WriteLine("UnpackToFolder. Not enough memory!");
                            return -1;
                        }
                    }
                }
                
                ret = LoadFile(pFileData, FileDataSize, false);

                if (ret == V8UNPACK_NOT_V8_FILE) {
                    Console.WriteLine("UnpackToFolder. This is not V8 file!");
                    return ret;
                }
                if (ret == V8UNPACK_NOT_V8_FILE) {
                    Console.WriteLine("UnpackToFolder. Error in load file in memory!");
                    return ret;
                }

                try
                {
                    System.IO.DirectoryInfo dirUnpack = new DirectoryInfo(dirName);
                    dirUnpack.Create();
                }
                catch
                {
                    Console.WriteLine("UnpackToFolder. Error in creating directory!");
                    return ret;
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
                    return ret;
                }               

                UInt32 onePercent = (UInt32)Elems.Count() / 50;
                int ElemsNum = Elems.Count();
                if (printProgress && onePercent != 0) {
                    Console.WriteLine("Progress (50 points): ");
                }

                foreach (CV8Elem elem in this.Elems)
                {
                    if (printProgress && ElemsNum != 0 && onePercent != 0 && ElemsNum % onePercent == 0) {
                        if (ElemsNum % (onePercent*10) == 0)
                            Console.WriteLine("|");
                        else
                            Console.WriteLine(".");
                    }

                    // если передано имя блока для распаковки, пропускаем все остальные
                    if (!string.IsNullOrEmpty(UnpackElemWithName) && (UnpackElemWithName != elem.elemName))
                        continue;

                    filename_out = dirName;
                    filename_out += "/";
                    filename_out += elem.elemName;
                    filename_out += ".header";

                    System.IO.File.WriteAllBytes(filename_out, elem.pHeader);

                    filename_out = dirName;
                    filename_out += "/";
                    filename_out += elem.elemName;
                    filename_out += ".data";

                    System.IO.File.WriteAllBytes(filename_out, elem.pData);
                }
                
                if (printProgress && onePercent != 0) {
                    Console.WriteLine();
                }

                return 0;
            }

            public int PackFromFolder(string dirname, string outFileName)
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
                    CV8Elem elem = new CV8Elem();

                    filename = file;
                    byte[] fileHeader = File.ReadAllBytes(filename);

                    elem.HeaderSize = (UInt32)fileHeader.Length;
                    elem.pHeader = fileHeader;

                    filename = filename.Replace(".header", ".Data");
                    byte[] fileData = File.ReadAllBytes(filename);

                    elem.DataSize = (UInt32)fileData.Length;
                    elem.pData = fileData;                   

                    Elems.Add(elem);
                }

                SaveFile(outFileName);

                return 0;
            }

            public int Parse(string filename, string dirname, int level = 0)
            {
                byte[] pFileData = null;

                int ret = 0;
                int FileDataSize = 0;

                FileInfo filePath = new FileInfo(filename);
                if (!filePath.Exists)
                {
                    Console.WriteLine("UnpackToFolder. Input file not found!");
                    return -1;
                }

                using (FileStream fileInStream = new FileStream(filePath.FullName, FileMode.Open))
                {
                    using (BinaryReader fileIn = new BinaryReader(fileInStream))
                    {
                        FileDataSize = (int)filePath.Length;

                        byte[] sz_r = fileIn.ReadBytes(FileDataSize);
                        if (sz_r.Length != FileDataSize)
                        {
                            Console.WriteLine("UnpackToFolder. Error in reading file!");
                            return sz_r.Length;
                        }
                        pFileData = sz_r;
                        if (pFileData.Length == 0)
                        {
                            Console.WriteLine("UnpackToFolder. Not enough memory!");
                            return -1;
                        }
                    }
                }

                ret = LoadFile(pFileData, FileDataSize);
                Console.WriteLine("LoadFile: ok\n");

                if (ret == V8UNPACK_NOT_V8_FILE)
                {
                    Console.WriteLine("UnpackToFolder. This is not V8 file!");
                    return ret;
                }
                if (ret == V8UNPACK_NOT_V8_FILE)
                {
                    Console.WriteLine("UnpackToFolder. Error in load file in memory!");
                    return ret;
                }

                if (pFileData.Length == 0)
                    pFileData = null;

                ret = SaveFileToFolder(dirname, pFileData);

                return ret;
            }

            int SaveFileToFolder(string dirName, byte[] pFileData)
            {
	            int ret = 0;
                
                try
                {
                    System.IO.DirectoryInfo dirUnpack = new DirectoryInfo(dirName);
                    dirUnpack.Create();
                }
                catch
                {
                    Console.WriteLine("UnpackToFolder. Error in creating directory!");
                    return ret;
                }

                string filename_out;

                UInt32 ElemsNum = (UInt32)this.Elems.Count;

                bool printProgress = true;
                UInt32 onePercent = ElemsNum / 50;
                if (printProgress && onePercent != 0)
                {
                    Console.WriteLine("Progress (50 points): ");
                }
                
                foreach(CV8Elem elem in this.Elems)
                {
                    int ElemNum = this.Elems.IndexOf(elem);

                    if (printProgress && ElemNum != 0 && onePercent != 0 && ElemNum % onePercent == 0)
                    {
                        if (ElemNum % (onePercent * 10) == 0)
                            Console.Write("|");
                        else
                            Console.Write(".");
                    }

                    filename_out = string.Format("{0}\\{1}", dirName, elem.elemName);

                    if (!elem.IsV8File)
                    {
                        try
                        {
                            System.IO.File.WriteAllBytes(filename_out, elem.pData);
                        }
                        catch
                        {
                            Console.WriteLine("UnpackToFolder. Error in creating file!");
                            return ret;
                        }   
                    }
                    else
                    {
                        ret = elem.UnpackedData.SaveFileToFolder(filename_out, pFileData);
                        if (ret != 0)
                            break;
                    }
                }

                if (printProgress && onePercent != 0)
                {
                    Console.WriteLine();
                }

	            return ret;
            }

            int LoadFile(byte[] pFileData, int FileDataSize, bool boolInflate = true, bool UnpackWhenNeed = true)
            {
                int ret = 0;

                if (pFileData.Length == 0) {
                    return V8UNPACK_ERROR;
                }

                bool isV8File = IsV8File(pFileData, FileDataSize);
                if (!isV8File)
                {
                    return V8UNPACK_NOT_V8_FILE;
                }

                byte[] InflateBuffer = new byte[0];
                UInt32 InflateSize = 0;

                this.FileHeader = new stFileHeader(pFileData, 0);

                stBlockHeader pBlockHeader = new stBlockHeader(pFileData, stFileHeader.Size());

                UInt32 ElemsAddrsSize;                
                byte[] pElemsAddrsBytes;
                ReadBlockData(pFileData, pBlockHeader, stFileHeader.Size(), out pElemsAddrsBytes, out ElemsAddrsSize);

                UInt32 ElemsNum = ElemsAddrsSize / stElemAddr.Size();

                Elems.Clear();
                ElemsAddrs.Clear();
                                
                for (UInt32 i = 0; i < ElemsNum; i++) {

                    stElemAddr pElemsAddrs = new stElemAddr(pElemsAddrsBytes, (int)(i * stElemAddr.Size()));
                    ElemsAddrs.Add(pElemsAddrs);

                    if (pElemsAddrs.fffffff != V8_FF_SIGNATURE)
                    {
                        ElemsNum = i;
                        break;
                    }

                    pBlockHeader = new stBlockHeader(pFileData, pElemsAddrs.elem_header_addr);

                    if (pBlockHeader.EOL_0D != 0x0d ||
                            pBlockHeader.EOL_0A != 0x0a ||
                            pBlockHeader.space1 != 0x20 ||
                            pBlockHeader.space2 != 0x20 ||
                            pBlockHeader.space3 != 0x20 ||
                            pBlockHeader.EOL2_0D != 0x0d ||
                            pBlockHeader.EOL2_0A != 0x0a)
                    {

                        ret = V8UNPACK_HEADER_ELEM_NOT_CORRECT;
                        break;
                    }

                    UInt32 ElemsAddrsSizeHeader = 0;
                    byte[] pElemsAddrsBytesHeader = new byte[0];
                    UInt32 DataSize = 0;
                    byte[] pData = new byte[0];
                    ReadBlockData(pFileData, pBlockHeader, pElemsAddrs.elem_header_addr, out pElemsAddrsBytesHeader, out ElemsAddrsSizeHeader);                    

                    //080228 Блока данных может не быть, тогда адрес блока данных равен 0x7fffffff
                    if (pElemsAddrs.elem_data_addr != V8_FF_SIGNATURE)
                    {
                        pBlockHeader = new stBlockHeader(pFileData, pElemsAddrs.elem_data_addr);
                        ReadBlockData(pFileData, pBlockHeader, pElemsAddrs.elem_data_addr, out pData, out DataSize);
                    }
                    else
                    {
                        throw new Exception("Ебать копать!!!");
                        //ReadBlockData(pFileData, null, out pData, out DataSize);
                    }

                    CV8Elem elem = new CV8Elem(pElemsAddrsBytesHeader, ElemsAddrsSizeHeader, pData, (UInt32)pData.Length, new V8Formats.CONF_ERF_EPF(), false, false);

                    if (boolInflate && IsDataPacked) {
                        ret = Inflate(pData, out InflateBuffer, DataSize, out InflateSize);

                        if (ret != 0)
                        {
                            IsDataPacked = false;
                            elem.pData = new byte[InflateSize];
                            elem.DataSize = InflateSize;
                            InflateBuffer.CopyTo(elem.pData, 0);
                            elem.IsV8File = false;
                            }
                        else {
                            elem.NeedUnpack = false; // отложенная распаковка не нужна
                            elem.pData = null; //нераспакованные данные больше не нужны
                            if (IsV8File(InflateBuffer, (int)InflateSize)) {
                                elem.UnpackedData = new CONF_ERF_EPF(InflateBuffer, (int)InflateSize, boolInflate);
                                elem.pData = null;
                                elem.IsV8File = true;
                            } else {
                                elem.pData = new byte[InflateSize];
                                elem.DataSize = InflateSize;
                                InflateBuffer.CopyTo(elem.pData, 0);
                                elem.IsV8File = false;
                            }
                            ret = 0;
                        }
                    }

                    elem.InitElemName(pFileData, pElemsAddrs);
                    Elems.Add(elem);

                }


                if (InflateBuffer.Length != 0)
                    InflateBuffer = new byte[0];

                return ret;
            }

            UInt32 _httoi(byte[] value)
            {
                UInt32 result = 0;
                
                string newByte = System.Text.Encoding.Default.GetString(value);
                result = UInt32.Parse(newByte, System.Globalization.NumberStyles.HexNumber);                         

                return result;
            }

            byte[] _intTo_BytesChar(UInt32 value)
            {
                string valueString = ConversionClass.IntToHexString((int)value, 8).ToLower();
                byte[] resultBytes = new byte[8];

                for (int i = 0; i < valueString.Length; i++)
                    resultBytes[i] = Convert.ToByte(valueString[i]);

                return resultBytes;
            }

            byte[] _inttobytes(UInt32 value)
            {
                string valueString = ConversionClass.IntToHexString((int)value, 8).ToUpper();

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

            int ReadBlockData(byte[] pFileData, stBlockHeader? pBlockHeader, UInt32 elemHeaderAddr, out byte[] pBlockData, out UInt32 BlockDataSize)
            {
                if (pBlockHeader == null)
                    pBlockHeader = new stBlockHeader();

                UInt32 data_size = 0, page_size = 0, next_page_addr = 0;
                UInt32 read_in_bytes = 0, bytes_to_read = 0;
                
                BlockDataSize = 0;
                pBlockData = new byte[0];

                if (pBlockHeader != null)
                {
                    data_size = _httoi(((stBlockHeader)pBlockHeader).data_size_hex);
                    if (data_size == 0)
                    {
                        Console.WriteLine("ReadBlockData. Block мData == NULL.");
                        return -1;
                    }
                    else
                    {
                        pBlockData = new byte[data_size];
                    }
                }
                read_in_bytes = 0;
                UInt32 adr = elemHeaderAddr + stBlockHeader.Size(); // Конец header
                while (read_in_bytes < data_size) {
                    page_size = _httoi(((stBlockHeader)pBlockHeader).page_size_hex);
                    next_page_addr = _httoi(((stBlockHeader)pBlockHeader).next_page_addr_hex);
                    
                    bytes_to_read = Math.Min(page_size, data_size - read_in_bytes);

                    Array.Copy(pFileData, adr, pBlockData, read_in_bytes, bytes_to_read);
                    read_in_bytes += bytes_to_read;

                    if (next_page_addr != V8Formats.CONF_ERF_EPF.V8_FF_SIGNATURE) // есть следующая страница
                    {
                        adr = next_page_addr + stBlockHeader.Size();
                        pBlockHeader = new stBlockHeader(pFileData, next_page_addr);
                    } else
                        break;
                }

                BlockDataSize = data_size;

                return 0;
            }

            bool IsV8File(byte[] pFileData, int FileDataSize)
            {
                if (pFileData.Length == 0)
                {
                    return false;
                }

                // проверим чтобы длина файла не была меньше длины заголовка файла и заголовка блока адресов
                if (FileDataSize < stFileHeader.Size() + stBlockHeader.Size())
                    return false;

                stBlockHeader pBlockHeader = new stBlockHeader(pFileData, 16);

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

            class CV8Elem
            {
                public CV8Elem()
                {
                    this.IsV8File = false;
                    this.HeaderSize = 0;
                    this.DataSize = 0;
                }

                public CV8Elem(byte[] pHeader, UInt32 HeaderSize, byte[] pData, UInt32 DataSize, CONF_ERF_EPF UnpackedData, bool IsV8File, bool NeedUnpack)
                {
                    this.pHeader = pHeader;
                    this.HeaderSize = HeaderSize;
                    this.pData = pData;
                    this.DataSize = DataSize;
                    this.UnpackedData = UnpackedData;
                    this.IsV8File = IsV8File;
                    this.NeedUnpack = NeedUnpack;
                }

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

                public struct stElemHeaderBegin
                {
                    public static uint Size()
                    {
                        return 8 + 8 + 4;
                    }
                }

                public string elemName;
                public UInt32 elemNameLen;

                public byte[] pHeader; // TODO: Утечка памяти
                public UInt32 HeaderSize;
                public byte[] pData; // TODO: Утечка памяти
                public UInt32 DataSize;
                public CONF_ERF_EPF UnpackedData;
                public bool IsV8File;
                public bool NeedUnpack;

            }
        }

        public class GRS
        {

        }

        public class MXL
        {

        }
    }
}
