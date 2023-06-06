package utils

import (
	"os/exec"
	"testing"
)

func TestRun(t *testing.T) {

	// Just make sure there is no error...
	testCmd := exec.Command("ls")
	err := Run(testCmd, nil)
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}
}
