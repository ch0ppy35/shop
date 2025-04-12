package main

import (
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strings"
)

// secureFileSystem is a wrapper around http.FileSystem that prevents
// directory traversal attacks by checking if the path is contained
// within the root directory
type secureFileSystem struct {
	fs      http.FileSystem
	rootDir string
}

func (sfs secureFileSystem) Open(path string) (http.File, error) {
	cleanedPath := filepath.Clean(path)

	if strings.HasPrefix(cleanedPath, "../") {
		return nil, os.ErrPermission
	}

	f, err := sfs.fs.Open(cleanedPath)
	if err != nil {
		return nil, err
	}

	stat, err := f.Stat()
	if err != nil {
		f.Close()
		return nil, err
	}

	if stat.IsDir() {
		absPath, err := filepath.Abs(filepath.Join(sfs.rootDir, cleanedPath))
		if err != nil {
			f.Close()
			return nil, err
		}

		if !strings.HasPrefix(absPath, sfs.rootDir) {
			f.Close()
			return nil, os.ErrPermission
		}
	}

	return f, nil
}

type customFileServer struct {
	root http.FileSystem
}

func (fs *customFileServer) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if strings.HasSuffix(r.URL.Path, ".wasm") {
		w.Header().Set("Content-Type", "application/wasm")
	}

	http.FileServer(fs.root).ServeHTTP(w, r)
}

func main() {
	port := flag.String("port", "8080", "Port to serve on")
	dir := flag.String("dir", "/app", "Directory to serve files from")
	flag.Parse()

	if envPort := os.Getenv("SERVER_PORT"); envPort != "" {
		*port = envPort
	}
	if envDir := os.Getenv("SERVE_DIR"); envDir != "" {
		*dir = envDir
	}

	absRootDir, err := filepath.Abs(*dir)
	if err != nil {
		log.Fatalf("Error getting absolute path: %v", err)
	}

	secureFS := secureFileSystem{
		fs:      http.Dir(*dir),
		rootDir: absRootDir,
	}

	fs := &customFileServer{root: secureFS}

	// Create a handler for SPA routing
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		path := r.URL.Path

		cleanedPath := filepath.Clean(path)
		if cleanedPath != path {
			http.Error(w, "Invalid path", http.StatusBadRequest)
			return
		}

		filePath := filepath.Join(*dir, path)
		_, err := os.Stat(filePath)

		if os.IsNotExist(err) &&
			!strings.HasPrefix(path, "/_framework/") &&
			!strings.HasPrefix(path, "/css/") &&
			!strings.HasPrefix(path, "/js/") &&
			!strings.HasPrefix(path, "/lib/") &&
			!strings.HasSuffix(path, ".ico") &&
			!strings.HasSuffix(path, ".png") {
			http.ServeFile(w, r, filepath.Join(*dir, "index.html"))
			return
		}

		fs.ServeHTTP(w, r)
	})

	// Start the server
	addr := fmt.Sprintf("0.0.0.0:%s", *port)
	log.Printf("Starting server on %s, serving files from %s", addr, *dir)
	log.Fatal(http.ListenAndServe(addr, nil))
}
