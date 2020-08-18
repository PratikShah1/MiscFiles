using System;
using System.Collections.Generic;
using System.IO;
using Bootstrap.Models;
using System.Web.Mvc;
using System.Linq;
using FolderBrowser.Models;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web;

namespace Bootstrap.Controllers
{
    public class TreeViewController : Controller
    {
        [HttpPost]
        public JsonResult TreeData(string baseDir)
        {
            //var patt = Server.MapPath(@"~\"+ baseDir);
            var browsingRoot = Path.Combine(baseDir, "");
            var nodes = new List<TreeNode>();
            nodes.AddRange(RecurseDirectory(browsingRoot));

            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public string GetServerPath(string baseDir)
        {
            var serverPath = Server.MapPath(@"~\" + baseDir);
            return serverPath;
        }


        [HttpPost]
        public ActionResult GetFiles(string directory)
        {
            var dirInfo = new DirectoryInfo(directory);
            var fileView = new List<FileViewModal>();
            var files = dirInfo.GetFiles();

            foreach (var file in files)
            {
                var fv = new FileViewModal();
                fv.FileName = file.Name;
                fv.FileType = file.Extension;
                fv.LastUpdated = file.LastAccessTime;
                fv.ActualSize = file.Length;
                fv.Path = file.FullName;
                string[] filePaths = file.FullName.Split(new[] { AppDomain.CurrentDomain.BaseDirectory }, StringSplitOptions.None);

                fv.ServerPath = filePaths[1]; 
                fileView.Add(fv);
            }

            return PartialView(fileView);
        }

        [HttpPost]
        public bool DeleteFiles(List<string> files)
        {
            try
            {
                foreach (var file in files)
                {
                    var filePath = AppDomain.CurrentDomain.BaseDirectory + file;
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e.Message);
            }
            return false;
        }

        [HttpPost]
        public bool CreateFolder(string folderName, string path)
        {
            Directory.CreateDirectory(path + "/" + folderName);
            return true;
        }

        // Adds an ACL entry on the specified directory for the specified account.
        public static void AddDirectorySecurity(string FileName, string Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            // Create a new DirectoryInfo object.
            DirectoryInfo dInfo = new DirectoryInfo(FileName);

            // Get a DirectorySecurity object that represents the
            // current security settings.
            DirectorySecurity dSecurity = dInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings.
            dSecurity.AddAccessRule(new FileSystemAccessRule(Account,
                                                            Rights,
                                                            ControlType));

            // Set the new access settings.
            dInfo.SetAccessControl(dSecurity);
        }

        [HttpPost]
        public bool DeleteFolder(string folderPath)
        {
            Directory.Delete(folderPath, true);
            return true;
        }

        [HttpPost]
        public bool FolderRename(string oldPath, string newName)
        {
            string lastFolderName = Path.GetDirectoryName(oldPath);
            string oldPath1 = new DirectoryInfo(oldPath).Name;
            Directory.Move(oldPath, lastFolderName + "\\" + newName);
            return true;
        }

        [HttpGet]
        public string GetFolderName(string path)
        {
            string folderName = new DirectoryInfo(path).Name;
            return folderName;
        }

        private List<TreeNode> RecurseDirectory(string directory)
        {
            var ret = new List<TreeNode>();
            var dirInfo = new DirectoryInfo(directory);


            try
            {
                var directories = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

                foreach (var dir in directories)
                {

                    if (dir.FullName.ToLower() == dirInfo.FullName)
                    {
                        continue;
                    }

                    var thisNode = TreeNode.FromDirInfo(dir);
                    thisNode.nodes.AddRange(RecurseDirectory(dir.FullName));
                    ret.Add(thisNode);
                }
            }
            catch (UnauthorizedAccessException ux)
            {
                // NB Log.
            }

            return ret;
        }


        [HttpPost]
        public ActionResult UploadFilesTempStorage(string directoryPath)
        {
            try
            {
                var tempFilesDirPath = Server.MapPath(@"~\" + "TempUploadFiles");
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
                Directory.CreateDirectory(tempFilesDirPath);

                //string path = directoryPath;
                HttpFileCollectionBase files = Request.Files;
                for (int i = 0; i < files.Count; i++)
                {
                    HttpPostedFileBase file = files[i];
                    var fileNameSave = Path.Combine(tempFilesDirPath, file.FileName);
                    file.SaveAs(fileNameSave);
                }

                return Json(new { isSuccess = true, successMessage = files.Count + " Files Uploaded!", directoryPath = directoryPath });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, successMessage = "Something went wrong. Please try again later!", directoryPath = directoryPath });
            }
        }


        [HttpPost]
        public ActionResult UploadFilesToDirectory(string directoryPath)
        {
            try
            {
             
                //string path = directoryPath;
                HttpFileCollectionBase files = Request.Files;
                for (int i = 0; i < files.Count; i++)
                {
                    HttpPostedFileBase file = files[i];
                    var fileNameSave = Path.Combine(directoryPath, file.FileName);
                    file.SaveAs(fileNameSave);
                }

                return Json(new { isSuccess = true, successMessage = files.Count + " Files Uploaded!", directoryPath = directoryPath });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, successMessage = "Something went wrong. Please try again later!", directoryPath = directoryPath });
            }
        }


        [HttpPost]
        public ActionResult UploadFiles(string directoryPath, List<string> files)
        {
            try
            {
                string path = directoryPath;
               

                foreach (var filename in files)
                {
                    string file = filename.Replace("TempUploadFiles\\","").ToString();

                    //Do your job with "file"  
                    var tempFilesDirPath = Server.MapPath(@"~\" + "TempUploadFiles");
                    var srcPath = Path.Combine(tempFilesDirPath, file);
                    var fileNameSave = Path.Combine(path, file);

                    if (!System.IO.File.Exists(fileNameSave))
                    {
                        System.IO.File.Copy(srcPath, fileNameSave);
                    }
                }
                return Json(new { isSuccess = true ,successMessage = "Files uploaded successfully!", directoryPath = directoryPath });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, successMessage = "Something went wrong. Please try again later!", directoryPath = directoryPath });
            }
        }

        [HttpPost]
        public ActionResult UploadedFilesGrid(string directoryPath)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var fileView = new List<FileViewModal>();
            var files = dirInfo.GetFiles();

            foreach (var file in files)
            {
                var fv = new FileViewModal();
                fv.FileName = file.Name;
                fv.FileType = file.Extension;
                fv.LastUpdated = file.LastAccessTime;
                fv.ActualSize = file.Length;
                fv.Path = file.FullName;
                string[] filePaths = file.FullName.Split(new[] { AppDomain.CurrentDomain.BaseDirectory }, StringSplitOptions.None);

                fv.ServerPath = filePaths[1];
                fileView.Add(fv);
            }
            return PartialView(fileView);
        }

        [HttpPost]
        public bool DeleteTempFiles(List<string> files)
        {
            try
            {
                foreach (var file in files)
                {
                    var filePath =  Path.Combine(Server.MapPath(@"~\") + file);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e.Message);
            }
            return false;
        }
    }
}