load("@rules_proto//proto:defs.bzl", "proto_library")


proto_library(
      name = "openvectorformat_proto",
      srcs = ["open_vector_format.proto"],
      visibility = ["//visibility:public"],
)

proto_library(
      name = "ovf_lut_proto",
      srcs = ["ovf_lut.proto"],
      visibility = ["//visibility:public"],
)
cc_proto_library(
      name = "ovf_proto_cpp",
      deps = [":openvectorformat_proto"],
      visibility = ["//visibility:public"],
)

cc_proto_library(
      name = "ovf_lut_proto_cpp",
      deps = [":ovf_lut_proto"],
      visibility = ["//visibility:public"],
)


