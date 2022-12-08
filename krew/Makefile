.DEFAULT_GOAL:=help
SHELL := /usr/bin/env bash

ROOT_DIR := $(shell git rev-parse --show-toplevel)
GOOS ?= $(shell go env GOOS)
GOARCH ?= $(shell go env GOARCH)
BIN_NAME ?= kubectl-bilipro
KUBECTL_DIR ?= $(shell which kubectl | awk -F 'kubectl' '{printf "%s\n", $$1 }')



GITCOMMIT ?= `git rev-parse HEAD`

help: #### display help
	@awk 'BEGIN {FS = ":.*## "; printf "\nTargets:\n"} /^[a-zA-Z_-]+:.*?#### / { printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)
	@awk 'BEGIN {FS = ":.* ## "; printf "\n  \033[1;32mBuild targets\033[36m\033[0m\n  \033[0;37mTargets for building and/or installing CLI plugins on the system.\n  Append \"ENVS=<os-arch>\" to the end of these targets to limit the binaries built.\n  e.g.: make build-all-tanzu-cli-plugins ENVS=linux-amd64  \n  List available at https://github.com/golang/go/blob/master/src/go/build/syslist.go\033[36m\033[0m\n\n"} /^[a-zA-Z_-]+:.*? ## / { printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)
##### GLOBAL

.PHONY: deploy
deploy: build install #### build + install


.PHONY: build
build: #### build the plugin
	@go vet ./... && \
	go fmt ./... && \
	cd ${ROOT_DIR}/krew/cmd && \
		GOOS=${GOOS} GOARCH=${GOARCH} CGO_ENABLED=0 go build -ldflags "-X github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/cmd.Version=${GITCOMMIT}" -o $(BIN_NAME) kubectl-bilipro.go


.PHONY: install
install: #### install the plugin
	@cd ${ROOT_DIR}/krew/cmd && \
      sudo install ./$(BIN_NAME) ${KUBECTL_DIR}/$(BIN_NAME)