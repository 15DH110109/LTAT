using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace Client
{
    class DiffieHellman : IDisposable
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

            //Khóa public
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
        //properties để lấy Initvector
        public byte[] IV
        {
            get
            {
                return this.aes.IV;
            }
        }

        public void LayKhoaBiMat(byte[] key)
        {
            var keym = CngKey.Import(key, CngKeyBlobFormat.EccPublicBlob); // tạo khóa bí mật từ khóa public (Khóa public này là của server)
            keychung = this.diffieHellman.DeriveKeyMaterial(keym); // khóa bí mật tương đương với key chung, đây là common secret  
            this.aes.Key = keychung; // add vào properties lúc này khóa public sẽ nằm ở Aes.key          
        }

        //Mã hóa tin nhắn
        public byte[] MaHoaDiffie(byte[] publicKey, string secretMessage)
        {
            byte[] encryptedMessage;
            using (var cipherText = new MemoryStream())
            {
                using (var encryptor = this.aes.CreateEncryptor())
                {
                    using (var cryptoStream = new CryptoStream(cipherText, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] ciphertextMessage = Encoding.UTF8.GetBytes(secretMessage); // mã hóa tin nhắn
                        cryptoStream.Write(ciphertextMessage, 0, ciphertextMessage.Length);
                    }
                }

                encryptedMessage = cipherText.ToArray();
            }

            return encryptedMessage; // tin nhắn được mã hóa dưới dạng byte[]
        }

        public string GiaiMaDiffie(byte[] publicKey, byte[] encryptedMessage, byte[] iv)
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

        //Hàm giải tỏa bộ nhớ khi xong
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //Hàm giải tỏa bộ nhớ khi xong
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

