package utils

import (
	"io"
	"io/fs"
	"log"
	"path"
	"strings"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/resources"
	"sigs.k8s.io/kustomize/kyaml/filesys"
)

var assetFS = resources.GetStaticResources()

// GetResourceFileSys file
func GetResourceFileSys() (filesys.FileSystem, error) {
	inDiskSys := filesys.MakeFsOnDisk()
	// copy from the resources into the target folder on the in memory FS
	if err := copyDirtoDiskFS(".", "bilipro", inDiskSys); err != nil {
		log.Println(err)
		return nil, err
	}
	return inDiskSys, nil
}

func copyFileToDiskFS(src, dst string, diskFS filesys.FileSystem) error {
	// skip all .go files
	if strings.HasSuffix(src, ".go") {
		return nil
	}
	var err error
	var srcFileDesc fs.File
	var dstFileDesc filesys.File

	if srcFileDesc, err = assetFS.Open(src); err != nil {
		return err
	}
	defer srcFileDesc.Close()

	if dstFileDesc, err = diskFS.Create(dst); err != nil {
		return err
	}
	defer dstFileDesc.Close()

	// Note: I had to read the whole string, for some reason io.Copy was not copying the whole content
	input, err := io.ReadAll(srcFileDesc)
	if err != nil {
		return err
	}

	_, err = dstFileDesc.Write(input)
	return err
}

func copyDirtoDiskFS(src string, dst string, diskFS filesys.FileSystem) error {
	var err error
	var fds []fs.DirEntry

	if err = diskFS.MkdirAll(dst); err != nil {
		return err
	}

	if fds, err = assetFS.ReadDir(src); err != nil {
		return err
	}
	for _, fd := range fds {
		srcfp := path.Join(src, fd.Name())
		dstfp := path.Join(dst, fd.Name())

		if fd.IsDir() {
			if err = copyDirtoDiskFS(srcfp, dstfp, diskFS); err != nil {
				return err
			}
		} else {
			if err = copyFileToDiskFS(srcfp, dstfp, diskFS); err != nil {
				return err
			}
		}
	}
	return nil
}
