using System;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Advanced
{
    /// <summary>
    /// Representation of a git PackBuilder.
    /// </summary>
    public sealed class PackBuilder : IDisposable
    {
        private PackBuilderSafeHandle packBuilderHandle;
        private readonly RepositorySafeHandle repositoryHandle;

        /// <summary>
        /// Constructs a PackBuilder for a <see cref="Repository"/>.
        /// </summary>
        public PackBuilder(Repository repository)
        {
            Ensure.ArgumentNotNull(repository, "repository");

            repositoryHandle = repository.Handle;
            packBuilderHandle = Proxy.git_packbuilder_new(repositoryHandle);
        }

        /// <summary>
        /// Inserts a single <see cref="GitObject"/> to the PackBuilder.
        /// For an optimal pack it's mandatory to insert objects in recency order, commits followed by trees and blobs. (quoted from libgit2 API ref)
        /// </summary>
        /// <param name="gitObject">The object to be inserted.</param>
        /// <exception cref="ArgumentNullException">if the gitObject is null</exception>
        public void Add<T>(T gitObject) where T : GitObject
        {
            Ensure.ArgumentNotNull(gitObject, "gitObject");

            Add(gitObject.Id);
        }

        /// <summary>
        /// Recursively inserts a <see cref="GitObject"/> and its referenced objects.
        /// Inserts the object as well as any object it references.
        /// </summary>
        /// <param name="gitObject">The object to be inserted recursively.</param>
        /// <exception cref="ArgumentNullException">if the gitObject is null</exception>
        public void AddRecursively<T>(T gitObject) where T : GitObject
        {
            Ensure.ArgumentNotNull(gitObject, "gitObject");

            AddRecursively(gitObject.Id);
        }

        /// <summary>
        /// Inserts a single object to the PackBuilder by its <see cref="ObjectId"/>.
        /// For an optimal pack it's mandatory to insert objects in recency order, commits followed by trees and blobs. (quoted from libgit2 API ref)
        /// </summary>
        /// <param name="id">The object ID to be inserted.</param>
        /// <exception cref="ArgumentNullException">if the id is null</exception>
        public void Add(ObjectId id)
        {
            Ensure.ArgumentNotNull(id, "id");

            Proxy.git_packbuilder_insert(packBuilderHandle, id, null);
        }

        /// <summary>
        /// Recursively inserts an object and its referenced objects by its <see cref="ObjectId"/>.
        /// Inserts the object as well as any object it references.
        /// </summary>
        /// <param name="id">The object ID to be recursively inserted.</param>
        /// <exception cref="ArgumentNullException">if the id is null</exception>
        public void AddRecursively(ObjectId id)
        {
            Ensure.ArgumentNotNull(id, "id");

            Proxy.git_packbuilder_insert_recur(packBuilderHandle, id, null);
        }

        /// <summary>
        /// Disposes the PackBuilder object.
        /// </summary>
        void IDisposable.Dispose()
        {
            packBuilderHandle.SafeDispose();
        }

        /// <summary>
        /// Writes the pack file and corresponding index file to path.
        /// </summary>
        /// <param name="packDirectoryPath">The directory path that pack and index files will be written to it.</param>
        public PackBuilderResults WritePackTo(string packDirectoryPath)
        {
            Ensure.ArgumentNotNullOrEmptyString(packDirectoryPath, "packDirectoryPath");

            if (!Directory.Exists(packDirectoryPath))
            {
                throw new DirectoryNotFoundException("The Directory " + packDirectoryPath + " does not exist.");
            }

            Proxy.git_packbuilder_write(packBuilderHandle, packDirectoryPath);

            return new PackBuilderResults(WrittenObjectsCount, PackHash);
        }

        public PackBuilderResults WritePackTo(Stream outStream)
        {
            // Leaving it for Kevin David.
            return new PackBuilderResults();
        }

        /// <summary>
        /// Sets number of threads to spawn.
        /// </summary>
        /// <returns> Returns the number of actual threads to be used.</returns>
        /// <param name="nThread">The Number of threads to spawn. An argument of 0 ensures using all available CPUs</param>
        public int SetMaximumNumberOfThreads(int nThread)
        {
            // Libgit2 set the number of threads to 1 by default, 0 ensures git_online_cpus
            return (int)Proxy.git_packbuilder_set_threads(packBuilderHandle, (uint)nThread);
        }

        /// <summary>
        /// Number of objects the PackBuilder will write out.
        /// </summary>
        public long ObjectsCount
        {
            get { return Proxy.git_packbuilder_object_count(packBuilderHandle); }
        }

        /// <summary>
        /// Gets the pack file's hash
        /// A pack file's name is derived from the sorted hashing of all object names. 
        /// This is only correct after the pack file has been written.
        /// </summary>
        internal string PackHash
        {
            get { return Proxy.git_packbuilder_hash(packBuilderHandle).Sha; }
        }

        /// <summary>
        /// Number of objects the PackBuilder has already written out. 
        /// This is only correct after the pack file has been written.
        /// </summary>
        internal long WrittenObjectsCount
        {
            get { return Proxy.git_packbuilder_written(packBuilderHandle); }
        }

        public void Reset()
        {
            // Dispose the old handle
            packBuilderHandle.SafeDispose();

            // Create a new handle
            packBuilderHandle = Proxy.git_packbuilder_new(repositoryHandle);
        }

        internal PackBuilderSafeHandle Handle
        {
            get { return packBuilderHandle; }
        }
    }

    /// <summary>
    /// The results of pack process of the <see cref="ObjectDatabase"/>.
    /// </summary>
    public struct PackBuilderResults
    {
        internal PackBuilderResults(long writtenObjectsCount, string packHash) : this()
        {
            WrittenObjectsCount = writtenObjectsCount;
            PackHash = packHash;
        }

        /// <summary>
        /// Number of objects that the PackBuilder has already written out.
        /// </summary>
        public long WrittenObjectsCount { get; internal set; }

        /// <summary>
        /// Hash of the pack file that the PackBuilder has already written out.
        /// </summary>
        public string PackHash { get; internal set; }
    }
}
