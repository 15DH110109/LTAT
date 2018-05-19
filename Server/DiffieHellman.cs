using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
namespace Server
{
    class DiffieHellman: IDisposable
    {
        public Aes aes = null;
        private ECDiffieHellmanCng diffieHellman = null;
        byte[] keychung;
        private readonly byte[] publicKey;

        //Gọi hàm diffiehellman để lấy khóa public
        public DiffieHellman()
        {
            this.aes = new AesCryptoServiceProvider();

            this.diffieHellman = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };
            
            this.publicKey = this.diffieHellman.PublicKey.ToByteArray();
        }
        //properties để lấy public key
        public byte[] PublicKey
        {
            get
            {
                return this.publicKey;
            }
        }
        //properties để lấy IV
        public byte[] IV
        {
            get
            {
                return this.aes.IV;
            }
        }
        public void LayKhoaBiMat(byte[] keyclient)
        {
            var key = CngKey.Import(keyclient, CngKeyBlobFormat.EccPublicBlob);
            keychung = this.diffieHellman.DeriveKeyMaterial(key); 
            this.aes.Key = keychung;           
        }
        public byte[] MaHoaMessage(byte[] publicKey, string secretMessage)
        {
            byte[] encryptedMessage;
            using (var cipherText = new MemoryStream())
            {
                using (var encryptor = this.aes.CreateEncryptor())
                {
                    using (var cryptoStream = new CryptoStream(cipherText, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] ciphertextMessage = Encoding.UTF8.GetBytes(secretMessage); 
                        cryptoStream.Write(ciphertextMessage, 0, ciphertextMessage.Length);
                    }
                }

                encryptedMessage = cipherText.ToArray();
            }

            return encryptedMessage;
        }
        public string GiaiMaMassage(byte[] publicKey, byte[] encryptedMessage, byte[] iv)
        {
            string decryptedMessage;
            this.aes.IV = iv;

            using (var plainText = new MemoryStream())
            {
                using (var decryptor = this.aes.CreateDecryptor())
                {
                    using (var cryptoStream = new CryptoStream(plainText, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(encryptedMessage, 0, encryptedMessage.Length);
                    }
                }
                decryptedMessage = Encoding.UTF8.GetString(plainText.ToArray());
            }

            return decryptedMessage;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.aes != null)
                    this.aes.Dispose();

                if (this.diffieHellman != null)
                    this.diffieHellman.Dispose();
            }
        }
    }
}
