cc_library(
	name = "OVFFileReaderWriter",
	
	hdrs = ["OVFFileReader.h",
	 	"FileReader.h",
	 	"IReader.h",
		"OVFFileWriter.h",
	 	"FileWriter.h",
	 	"IWriter.h",
		"copy_with_exclude.h"],
	deps = ["@ovf//:ovf_proto_cpp", "@ovf//:ovf_lut_proto_cpp"],
	)

cc_binary(
	name = "OVFReaderWriter_test",
	srcs = ["OVFReaderWriter_test.cpp"],
	deps = [":OVFFileReaderWriter",],
)
