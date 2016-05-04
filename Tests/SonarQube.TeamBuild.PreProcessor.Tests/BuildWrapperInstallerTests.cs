﻿//-----------------------------------------------------------------------
// <copyright file="BuildWrapperInstallerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TestUtilities;

namespace SonarQube.TeamBuild.PreProcessor.Tests
{
    [TestClass]
    public class BuildWrapperInstallerTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void BuildWrapper_PluginNotInstalled_NoDownload()
        {
            // Arrange
            string rootDir = TestUtils.CreateTestSpecificFolder(this.TestContext);
            TestLogger logger = new TestLogger();

            MockSonarQubeServer mockServer = new MockSonarQubeServer();

            BuildWrapperInstaller testSubject = new BuildWrapperInstaller(logger);

            // Act
            testSubject.InstallBuildWrapper(mockServer, rootDir);

            // Assert
            logger.AssertSingleInfoMessageExists(SonarQube.TeamBuild.PreProcessor.Resources.BW_CppPluginNotInstalled);
            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);

            AssertNoFilesExist(rootDir);
        }

        [TestMethod]
        public void BuildWrapper_OldCppPluginInstalled_FilesDownloaded()
        {
            // If an older version of the C++ plugin is installed then the embedded resource
            // won't exist. In that case we expect a warning message telling the user to upgrade.

            // Arrange
            string rootDir = TestUtils.CreateTestSpecificFolder(this.TestContext);
            TestLogger logger = new TestLogger();

            MockSonarQubeServer mockServer = new MockSonarQubeServer();
            mockServer.Data.InstalledPlugins.Add("cpp"); // plugin exists but no zip file

            BuildWrapperInstaller testSubject = new BuildWrapperInstaller(logger);

            // Act
            testSubject.InstallBuildWrapper(mockServer, rootDir);

            // Assert
            logger.AssertSingleWarningExists(SonarQube.TeamBuild.PreProcessor.Resources.BW_CppPluginUpgradeRequired);
            logger.AssertErrorsLogged(0);

            AssertNoFilesExist(rootDir);
        }

        [TestMethod]
        public void BuildWrapper_PluginInstalled_FilesDownloaded()
        {
            // Arrange
            string rootDir = TestUtils.CreateTestSpecificFolder(this.TestContext);
            TestLogger logger = new TestLogger();

            MockSonarQubeServer mockServer = new MockSonarQubeServer();
            mockServer.Data.InstalledPlugins.Add("cpp");

            // See https://jira.sonarsource.com/browse/CPP-1458 for the embedded resource name
            // The build wrapper installer doesn't care what the content of zip is, just that it exists
            mockServer.Data.AddEmbeddedZipFile("cpp",
                "build-wrapper-win-x86.zip",
                // Content file names
                "file1.txt", "file2.txt", "file3.txt");

            BuildWrapperInstaller testSubject = new BuildWrapperInstaller(logger);

            // Act
            testSubject.InstallBuildWrapper(mockServer, rootDir);

            // Assert
            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);

            AssertFileExists(rootDir, "file1.txt");
            AssertFileExists(rootDir, "file2.txt");
            AssertFileExists(rootDir, "file3.txt");
        }

        #endregion

        #region Private methods

        private static void AssertFileExists(string rootDir, string fileName)
        {
            string fullPath = Path.Combine(rootDir, fileName);
            Assert.IsTrue(File.Exists(fullPath), "Expected file does not exist: {0}", fileName);
        }

        private static void AssertNoFilesExist(string directory)
        {
            Assert.AreEqual(0, Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Length, "Not expecting any files to have been created");
        }

        #endregion
    }
}
