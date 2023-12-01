package utils

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"time"

	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/labels"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
)

func GetK8sClient() (*kubernetes.Clientset, *rest.Config, error) {
	var kubeconfig string

	envKubeConfig := os.Getenv("KUBECONFIG")
	if envKubeConfig == "" {
		home, err := os.UserHomeDir()
		if err != nil {
			return nil, nil, GenErrorMsg(SERVER_ERROR, err.Error())
		}
		kubeconfig = filepath.Join(home, ".kube", "config")
	} else {
		kubeconfig = envKubeConfig
	}

	// use the current context in kubeconfig
	config, err := clientcmd.BuildConfigFromFlags("", kubeconfig)
	if err != nil {
		return nil, nil, GenErrorMsg(SERVER_ERROR, err.Error())
	}
	config.QPS = float32(10.0)
	config.Burst = 20

	// create the clientset
	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		return nil, config, GenErrorMsg(SERVER_ERROR, err.Error())
	}
	return clientset, config, nil
}

func GetBiliName(client *kubernetes.Clientset, namespace, deploymentName string) (string, error) {
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Minute)
	defer cancel()
	deployment, err := client.AppsV1().Deployments(namespace).Get(ctx, deploymentName, metav1.GetOptions{})
	if err != nil {
		return "", GenErrorMsg(SERVER_ERROR, err.Error())
	}

	// we dont do much checks here
	selector := deployment.Spec.Selector.MatchLabels
	if len(selector) == 0 {
		return "", GenErrorMsg(SERVER_ERROR, "deployment doesn't have any selectors, please check the deploy template")
	}
	listOptions := metav1.ListOptions{
		LabelSelector: labels.Set(selector).String(),
	}
	pods, err := client.CoreV1().Pods(namespace).List(ctx, listOptions)
	if err != nil {
		return "", GenErrorMsg(SERVER_ERROR, "cannot list the pods with deploy selectors")
	}
	if len(pods.Items) != 1 {
		return "", GenErrorMsg(SERVER_ERROR, fmt.Sprintf("pod number is expected to be 1, currently %d", len(pods.Items)))
	}

	// only one pod is supposed to be existing, soft constraint
	return pods.Items[0].ObjectMeta.GetName(), nil
}
