using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace ClientServer
{
    class CryptoManager
    {
        private byte[] mKey;
        private byte[] mIV;
        private RijndaelManaged mCrypto;
        private ICryptoTransform mEncryptor;
        private ICryptoTransform mDecryptor;

        public CryptoManager(long Key)
        {
            mCrypto = new RijndaelManaged();
            mCrypto.Padding = PaddingMode.Zeros;
            byte[] keyBytes = BitConverter.GetBytes(Key);
            mKey = new byte[16];
            mIV = new byte[16];
            Array.Copy(keyBytes, 0, mKey, 0, 8);
            Array.Copy(keyBytes, 0, mKey, 8, 8);
            Array.Clear(mIV, 0, 16);
            mEncryptor = mCrypto.CreateEncryptor(mKey, mIV);
            mDecryptor = mCrypto.CreateDecryptor(mKey, mIV);
        }

        public CryptoManager(long Key, long IV)
        {
            mCrypto = new RijndaelManaged();
            mCrypto.Padding = PaddingMode.Zeros;
            byte[] keyBytes = BitConverter.GetBytes(Key);
            byte[] ivBytes = BitConverter.GetBytes(IV);
            mKey = new byte[16];
            mIV = new byte[16];
            Array.Copy(keyBytes, 0, mKey, 0, 8);
            Array.Copy(keyBytes, 0, mKey, 8, 8);
            Array.Copy(ivBytes, 0, mIV, 0, 8);
            Array.Copy(ivBytes, 0, mIV, 8, 8);
            mEncryptor = mCrypto.CreateEncryptor(mKey, mIV);
            mDecryptor = mCrypto.CreateDecryptor(mKey, mIV);
        }


        public byte[] Encrypt(byte[] plaintext)
        {
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, mEncryptor, CryptoStreamMode.Write);
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            cs.Close();
            ms.Flush();
            ms.Close();
            return ms.ToArray();
        }

        public byte[] Decrypt(byte[] ciphertext)
        {
            MemoryStream ms = new MemoryStream(ciphertext);
            CryptoStream cs = new CryptoStream(ms, mDecryptor, CryptoStreamMode.Read);
            byte[] buf = new byte[ciphertext.Length];
            cs.Read(buf, 0, ciphertext.Length);
            cs.Close();
            ms.Flush();
            ms.Close();
            return buf;
        }
    }
}
