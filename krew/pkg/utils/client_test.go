package utils

import (
	"testing"
)

func TestGetK8sClient(t *testing.T) {
	client, _, err := GetK8sClient()
	if err != nil {
		t.Logf("test failed due to %s", err.Error())
		t.FailNow()
	}

	if client == nil {
		t.Logf("test failed for returned client is empty, this should not happen")
		t.FailNow()
	}

}
