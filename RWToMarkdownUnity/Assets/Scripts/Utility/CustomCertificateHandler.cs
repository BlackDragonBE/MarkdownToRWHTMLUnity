using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class CustomCertificateHandler : CertificateHandler
{
    // Encoded RSAPublicKey
    private static readonly string PUB_KEY = "";

    /// <summary>
    /// Validate the Certificate Against the Amazon public Cert
    /// </summary>
    /// <param name="certificateData">Certifcate to validate</param>
    /// <returns></returns>
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}