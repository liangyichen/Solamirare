namespace Solamirare.Encrypt
{

    /// <summary>
    /// 加密类别
    /// </summary>
    public enum EncryptType
    {
        /// <summary>
        /// 
        /// </summary>
        MD5_32=0,

        /// <summary>
        /// 
        /// </summary>
        SHA1=2,
        /// <summary>
        /// 
        /// </summary>
        AES=3,



        /// <summary>
        /// 
        /// </summary>
        HMAC_512=6,

        /// <summary>
        /// 
        /// </summary>
        None = 8,

        /// <summary>
        /// 
        /// </summary>
        SHA256 = 9,

        /// <summary>
        /// 
        /// </summary>
        SHA3_512 = 10,

        /// <summary>
        /// 
        /// </summary>
        SHA512 = 11,
    }
}
