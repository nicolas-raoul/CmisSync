namespace CmisSync.Lib.Storage 
{
    ///
    ///<summary>Wrapper for DirectoryInfo<summary>
    ///
    public class DirectoryInfoWrapper : FileSystemInfoWrapper, IDirectoryInfo
    {
        public string FullName{get {return "";}}       
    }
}
