using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public interface IEncryptionHelper
    {
        string Encrypt(string text);
        string Decrypt(string cipherText);
    }
}
