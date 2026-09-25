package utils

import (
	"io"
	"os"
	"path/filepath"
	"testing"

	"sigs.k8s.io/kustomize/kyaml/filesys"
)

func TestCopyFiletoDiskFS(t *testing.T) {
	expectedFile, err := assetFS.Open("base/ns/namespace.yaml")
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	expectedOutput, err := io.ReadAll(expectedFile)
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	dir, err := os.Getwd()
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	inDiskSys := filesys.MakeFsOnDisk()

	err = copyFileToDiskFS("base/ns/namespace.yaml", filepath.Join(dir, "fixtures/testNamespace.yaml"), inDiskSys)
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	actualOutput, err := os.ReadFile(filepath.Join(dir, "fixtures/testNamespace.yaml"))
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	if string(expectedOutput) != string(actualOutput) {
		t.Logf("test failed due to copy file failed")
		t.FailNow()
	}
}

func TestCopyDirtoDiskFS(t *testing.T) {
	expectedFile, err := assetFS.Open("base/bilibiliPro/deployment.yaml")
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	expectedOutput, err := io.ReadAll(expectedFile)
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	dir, err := os.Getwd()
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	inDiskSys := filesys.MakeFsOnDisk()

	err = copyDirtoDiskFS("base/bilibiliPro", filepath.Join(dir, "fixtures"), inDiskSys)
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	actualOutput, err := os.ReadFile(filepath.Join(dir, "fixtures/deployment.yaml"))
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	if string(expectedOutput) != string(actualOutput) {
		t.Logf("test failed due to copy file failed")
		t.FailNow()
	}
}
