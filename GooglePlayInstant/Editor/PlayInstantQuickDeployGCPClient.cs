namespace GooglePlayInstant.Editor
{
    public abstract class QuickDeployTokenUtility
    {
        public abstract string GetAuthCode();
        public abstract string GetAccessToken();
    }
    
    public abstract class QuickDeployCloudUtility
    {
        public abstract void UploadBundle();
        public abstract void MakeBundlePublic();
    }
}