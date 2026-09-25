package utils

import (
	"bufio"
	"fmt"
	"io"
	"os/exec"
)

func Run(cmd *exec.Cmd, in io.Reader) error {
	cmd.Stdin = in

	stdoutReader, _ := cmd.StdoutPipe()
	stdoutScanner := bufio.NewScanner(stdoutReader)
	go func() {
		for stdoutScanner.Scan() {
			fmt.Println(stdoutScanner.Text())
		}
	}()
	stderrReader, _ := cmd.StderrPipe()
	stderrScanner := bufio.NewScanner(stderrReader)
	go func() {
		for stderrScanner.Scan() {
			fmt.Println(stderrScanner.Text())
		}
	}()
	err := cmd.Start()
	if err != nil {
		return GenErrorMsg(EXEC_ERROR, err.Error())
	}

	// Stuck here until there are out and err
	err = cmd.Wait()
	if err != nil {
		return GenErrorMsg(EXEC_ERROR, err.Error())
	}
	return nil
}
