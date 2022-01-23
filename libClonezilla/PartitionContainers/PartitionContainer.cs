﻿using libClonezilla.Cache;
using libClonezilla.Partitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using lib7Zip;
using libClonezilla.PartitionContainers.ImageFiles;
using libDokan.VFS.Folders;
using libClonezilla.VFS;

namespace libClonezilla.PartitionContainers
{
    public abstract class PartitionContainer
    {
        public List<Partition> Partitions { get; protected set; } = new List<Partition>();

        public abstract string ContainerName { get; protected set; }

        public static PartitionContainer FromPath(string path, string cacheFolder, List<string> partitionsToLoad, bool willPerformRandomSeeking, IVFS vfs)
        {
            PartitionContainer? result = null;

            if (Directory.Exists(path))
            {
                var clonezillaMagicFile = Path.Combine(path, "clonezilla-img");

                if (File.Exists(clonezillaMagicFile))
                {
                    var clonezillaCacheManager = new ClonezillaCacheManager(path, cacheFolder);
                    result = new ClonezillaImage(path, clonezillaCacheManager, partitionsToLoad, willPerformRandomSeeking);
                }
            }
            else if (File.Exists(path))
            {
                using var fileStream = File.OpenRead(path);
                using var binaryReader = new BinaryReader(fileStream);
                var magic = Encoding.ASCII.GetString(binaryReader.ReadBytes(16)).TrimEnd('\0');

                if (magic.Equals("partclone-image"))
                {
                    result = new PartcloneFile(path, partitionsToLoad, willPerformRandomSeeking);
                }
                else
                {
                    result = new ImageFile(path, partitionsToLoad, willPerformRandomSeeking, vfs);
                }
            }

            if (result == null)
            {
                throw new Exception($"Could not determine if this is a Clonezilla folder, or a partclone file: {path}");
            }

            return result;
        }

        public static List<PartitionContainer> FromPaths(List<string> paths, string cacheFolder, List<string> partitionsToLoad, bool willPerformRandomSeeking, IVFS vfs)
        {
            var result = paths
                            .Select(path => FromPath(path, cacheFolder, partitionsToLoad, willPerformRandomSeeking, vfs))
                            .ToList();

            return result;
        }
    }
}