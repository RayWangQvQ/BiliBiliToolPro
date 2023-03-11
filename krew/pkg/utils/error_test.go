package utils

import (
	"strings"
	"testing"
)

func TestServerGenErrorMsg(t *testing.T) {
	expectedErr := GenErrorMsg(SERVER_ERROR, "test for server")
	if !strings.Contains(expectedErr.Error(), SERVER_ERROR) {
		t.Logf("server error generate failed")
		t.FailNow()
	}
}

func TestTemplateGenErrorMsg(t *testing.T) {
	expectedErr := GenErrorMsg(TEMPLATE_ERROR, "test for template")
	if !strings.Contains(expectedErr.Error(), TEMPLATE_ERROR) {
		t.Logf("template error generate failed")
		t.FailNow()
	}
}

func TestExecGenErrorMsg(t *testing.T) {
	expectedErr := GenErrorMsg(EXEC_ERROR, "test for exec")
	if !strings.Contains(expectedErr.Error(), EXEC_ERROR) {
		t.Logf("error error generate failed")
		t.FailNow()
	}
}

func TestFileGenErrorMsg(t *testing.T) {
	expectedErr := GenErrorMsg(FILE_ERROR, "test for file")
	if !strings.Contains(expectedErr.Error(), FILE_ERROR) {
		t.Logf("file error generate failed")
		t.FailNow()
	}
}
