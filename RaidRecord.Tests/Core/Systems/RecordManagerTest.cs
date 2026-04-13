using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaidRecord.Core.Systems;

namespace RaidRecord.Tests.Core.Systems;

[TestClass]
[TestSubject(typeof(RecordManager))]
public class RecordManagerTest
{

    [TestMethod]
    public void TestFuncIsSubFilePath()
    {
        // 测试 IsSubFilePath
        // 1. 正常情况：子文件在父目录下
        const string parent = @"C:\Test\RecordDb";
        const string subFile = @"C:\Test\RecordDb\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, subFile), "直接子文件应该返回 true");
        
        // 2. 深层子文件
        const string deepSubFile = @"C:\Test\RecordDb\SubFolder\Nested\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, deepSubFile), "深层子文件应该返回 true");
        
        // 3. 不在父目录下
        const string outsideFile = @"C:\Other\data.json";
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, outsideFile), "不在父目录下的文件应该返回 false");
        
        // 4. 路径包含相对路径 (..)
        const string relativePathFile = @"C:\Test\RecordDb\..\Other\data.json";
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, relativePathFile), "使用 .. 跳出目录应该返回 false");
        
        // 5. 路径包含相对路径 (在同一目录内)
        const string sameDirRelative = @"C:\Test\RecordDb\.\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, sameDirRelative), "使用 . 在同一目录内应该返回 true");
        
        // 6. 大小写测试（Windows 不区分大小写）
        const string parentMixed = @"C:\Test\Record";
        const string subFileMixed = @"C:\TEST\record\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parentMixed, subFileMixed), "大小写不同应该返回 true");
        
        // 7. 路径末尾带或不带路径分隔符
        const string parentWithSlash = @"C:\Test\RecordDb\";
        const string subFile2 = @"C:\Test\RecordDb\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parentWithSlash, subFile2), "父目录带结尾斜杠应该正常工作");
        
        // 8. 父目录和子文件完全相同(外部验证了文件不是目录)
        // const string samePath = @"C:\Test\RecordDb\data.json";
        // Assert.IsFalse(RecordManager.IsSubFilePath(samePath, samePath), "文件路径不能是父目录本身");
        
        // 9. 空路径或 null
        Assert.IsFalse(RecordManager.IsSubFilePath("", @"C:\Test\data.json"), "父目录为空字符串应该返回 false");
        Assert.IsFalse(RecordManager.IsSubFilePath(@"C:\Test", ""), "子文件为空字符串应该返回 false");
        
        // 10. 无效路径字符
        const string invalidPath = @"C:\Invalid|\Path\data.json";
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, invalidPath), "无效路径应该返回 false");
        
        // 11. UNC 路径
        const string uncParent = @"\\Server\Share\RecordDb";
        const string uncSubFile = @"\\Server\Share\RecordDb\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(uncParent, uncSubFile), "UNC 路径应该正常工作");
        
        // 12. Linux 风格路径（如果运行在 Linux 上）
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            const string linuxParent = "/home/user/RecordDb";
            const string linuxSubFile = "/home/user/RecordDb/data.json";
            Assert.IsTrue(RecordManager.IsSubFilePath(linuxParent, linuxSubFile), "Linux 路径应该正常工作");
            
            const string linuxOutside = "/home/user/Other/data.json";
            Assert.IsFalse(RecordManager.IsSubFilePath(linuxParent, linuxOutside), "Linux 路径不在目录下应该返回 false");
        }
        
        // 13. 父目录是子文件路径的前缀但实际不是子目录
        const string parentPrefix = @"C:\Test\RecordDb";
        const string similarFile = @"C:\Test\RecordDbBackup\data.json";
        Assert.IsFalse(RecordManager.IsSubFilePath(parentPrefix, similarFile), "仅是前缀但不是子目录应该返回 false");
        
        // 14. 相对路径父目录
        const string relativeParent = "RecordDb";
        string absoluteSubFile = Path.GetFullPath(@"RecordDb\data.json");
        Assert.IsTrue(RecordManager.IsSubFilePath(relativeParent, absoluteSubFile), "相对路径父目录应该能正常工作");
        
        Console.WriteLine("所有测试通过！");
    }
}