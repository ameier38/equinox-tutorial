package main

import (
	"fmt"
	"io"
	"net/http"

	"github.com/google/uuid"
	"github.com/minio/minio-go/v6"
	"github.com/nullseed/logruseq"
	log "github.com/sirupsen/logrus"
)

type uploader interface {
	upload(name string, file io.Reader, size int64) (string, error)
}

type handler struct {
	uploader uploader
}

func (h handler) health(w http.ResponseWriter, r *http.Request) {
	w.Write([]byte("Healthy!"))
	w.WriteHeader(http.StatusOK)
	w.Header().Set("Content-Type", "text/plain")
}

func (h handler) home(w http.ResponseWriter, r *http.Request) {
	if r.Method == "GET" {
		fmt.Fprintf(w, `<html>
<head>
  <title>GoLang HTTP Fileserver</title>
</head>
<body>
<h2>Upload a file</h2>
<form action="/upload" method="post" enctype="multipart/form-data">
  <label for="file">Filename:</label>
  <input type="file" name="file" id="file">
  <br>
  <input type="submit" name="submit" value="Submit">
</form>
</body>
</html>`)
	}
}

func (h handler) uploadFile(w http.ResponseWriter, r *http.Request) {
	file, header, err := r.FormFile("file")
	if err != nil {
		fmt.Fprintln(w, err)
		return
	}
	defer file.Close()
	fileID, err := uuid.NewUUID()
	if err != nil {
		fmt.Fprintln(w, err)
	}
	fileName := fmt.Sprintf("%s-%s", header.Filename, fileID.String())
	h.uploader.upload(fileName, file, header.Size)
	w.WriteHeader(http.StatusOK)
	w.Header().Set("Content-Type", "text/plain")
}

type s3Uploader struct {
	log    *log.Entry
	config s3Config
	client *minio.Client
}

func (s3 *s3Uploader) init(log *log.Entry, config s3Config) {
	log.Info("initializing minio client")
	s3Client, err := minio.New(config.endpoint, config.accessKey, config.secretKey, config.secure)
	if err != nil {
		log.Fatalf("could not initialize s3 client %v", err)
	}
	log.Infof("checking that bucket '%s' exists", config.uploadBucket)
	exists, err := s3Client.BucketExists(config.uploadBucket)
	if err != nil {
		log.Fatalf("error checking existence of bucket '%s' %v", config.uploadBucket, err)
	}
	if !exists {
		log.Infof("creating bucket '%s'", config.uploadBucket)
		s3Client.MakeBucket(config.uploadBucket, "")
	}
	s3.client = s3Client
	s3.log = log
	s3.config = config
}

func (s3 s3Uploader) upload(name string, file io.Reader, size int64) (string, error) {
	opts := minio.PutObjectOptions{ContentType: "application/octet-stream"}
	url := fmt.Sprintf("https://%s.%s/%s", s3.config.uploadBucket, s3.config.endpoint, name)
	log.Infof("uploading to %s", url)
	n, err := s3.client.PutObject(s3.config.uploadBucket, name, file, size, opts)
	if err != nil {
		return "", err
	}
	log.Infof("successfully uploaded %s of size %d", url, n)
	return url, nil
}

func main() {
	appLog := log.WithField("app", "upload-api")
	config := &config{}
	config.init()
	log.AddHook(logruseq.NewSeqHook(config.seq.url))
	s3Uploader := s3Uploader{}
	s3Uploader.init(appLog, config.s3)
	handler := handler{uploader: s3Uploader}
	appLog.Infof("🚀 listening at :%d", config.serverPort)
	appLog.Infof("📜 logs sent to %s", config.seq.url)
	http.HandleFunc("/_health", handler.health)
	http.HandleFunc("/upload", handler.uploadFile)
	http.HandleFunc("/", handler.home)
	appLog.Fatal(http.ListenAndServe(fmt.Sprintf(":%d", config.serverPort), nil))
}
