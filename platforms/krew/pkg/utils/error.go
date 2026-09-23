package utils

import (
	"errors"
	"fmt"
)

const (
	// For errors about kustomize
	TEMPLATE_ERROR = "template error"
	// For errors about file system
	FILE_ERROR = "file system error"
	// For errors about create/delete/... resources in cluster
	SERVER_ERROR = "cluster operation error"
	// For exec errors
	EXEC_ERROR = "exec error"
)

func GenErrorMsg(errType, customMsg string) error {
	errorMsg := fmt.Sprintf("[ERROR] %s: %s", errType, customMsg)
	return errors.New(errorMsg)
}
