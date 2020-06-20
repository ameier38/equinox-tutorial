package main

import (
	"fmt"
	"io/ioutil"
	"os"
	"path"
	"strconv"
	"strings"
)

func parseInt(s string, defaultValue int) int {
	value, err := strconv.Atoi(s)
	if err != nil {
		return defaultValue
	}
	return value
}

func getSecret(secretName string, secretKey string, defaultValue string) string {
	secretDir := os.Getenv("SECRET_DIR")
	secretPath := path.Join(secretDir, secretName, secretKey)
	data, err := ioutil.ReadFile(secretPath)
	if err != nil {
		return defaultValue
	}
	return string(data)
}

func getEnv(envVar string, defaultValue string) string {
	value := os.Getenv(envVar)
	if value == "" {
		return defaultValue
	}
	return value
}

func getS3Secret(secretKey string, envVar string, defaultValue string) string {
	defaultValue = getEnv(envVar, defaultValue)
	value := getSecret("s3", secretKey, defaultValue)
	return value
}

func getSeqSecret(secretKey string, envVar string, defaultValue string) string {
	defaultValue = getEnv(envVar, defaultValue)
	value := getSecret("seq", secretKey, defaultValue)
	return value
}

type s3Config struct {
	endpoint     string
	secure       bool
	accessKey    string
	secretKey    string
	uploadBucket string
}

func (c *s3Config) init() {
	scheme := getS3Secret("scheme", "S3_SCHEME", "http")
	host := getS3Secret("host", "S3_HOST", "localhost")
	port := getS3Secret("port", "S3_PORT", "9000")
	if port == "" || port == "80" {
		port = ""
	} else {
		port = fmt.Sprintf(":%s", port)
	}
	c.endpoint = fmt.Sprintf("%s%s", host, port)
	c.secure = scheme == "https"
	c.accessKey = getS3Secret("access-key", "S3_ACCESS_KEY", "TEST")
	c.secretKey = getS3Secret("secret-key", "S3_SECRET_KEY", "TEST")
	c.uploadBucket = getS3Secret("upload-bucket", "S3_UPLOAD_BUCKET", "upload")
}

type seqConfig struct {
	url string
}

func (c *seqConfig) init() {
	scheme := getSeqSecret("scheme", "SEQ_SCHEME", "http")
	host := getSeqSecret("host", "SEQ_HOST", "localhost")
	port := getSeqSecret("port", "SEQ_PORT", "5341")
	c.url = fmt.Sprintf("%s://%s:%s", scheme, host, port)
}

type config struct {
	debug      bool
	serverPort int
	s3         s3Config
	seq        seqConfig
}

func (c *config) init() {
	c.debug = strings.ToLower(os.Getenv("DEBUG")) == "true"
	c.serverPort = parseInt(os.Getenv("SERVER_PORT"), 8080)
	s3Config := &s3Config{}
	s3Config.init()
	c.s3 = *s3Config
	seqConfig := &seqConfig{}
	seqConfig.init()
	c.seq = *seqConfig
}
