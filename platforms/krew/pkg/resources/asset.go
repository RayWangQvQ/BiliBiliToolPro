package resources

import (
	"embed"
)

//go:embed *
var fs embed.FS

// GetStaticResources returns the fs with the embedded assets
func GetStaticResources() embed.FS {
	return fs
}
