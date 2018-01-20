internal static partial class ModelSecrets
{
    // TODO: TEMPORAL KEY. MUST BE REMOVED AND REVOKED.
    static ModelSecrets()
    {
        string gyazoId = null;
        string gyazoSecret = null;
        string imgurId = null;
        string imgurSecret = null;

        SetGyazoClientId(ref gyazoId);
        GYAZO_CLIENT_ID = gyazoId;

        SetGyazoClientSecret(ref gyazoSecret);
        GYAZO_CLIENT_SECRET = gyazoSecret;

        SetImgurClientId(ref imgurId);
        IMGUR_CLIENT_ID = imgurId;

        SetImgurClientSecret(ref imgurSecret);
        IMGUR_CLIENT_SECRET = imgurSecret;
    }

    internal static readonly string GYAZO_CLIENT_ID;
    internal static readonly string GYAZO_CLIENT_SECRET;
    internal static readonly string IMGUR_CLIENT_ID;
    internal static readonly string IMGUR_CLIENT_SECRET;
    static partial void SetGyazoClientId(ref string v);

    static partial void SetGyazoClientSecret(ref string v);

    static partial void SetImgurClientId(ref string v);

    static partial void SetImgurClientSecret(ref string v);
}