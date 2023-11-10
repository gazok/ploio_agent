cmake_minimum_required(VERSION 3.27)
project(fr_nf LANGUAGES CXX)

add_executable(fr_nf src/main.cc)
target_include_directories(fr_nf PRIVATE inc)
target_compile_options(fr_nf PRIVATE -W -Wall -Wextra)
target_compile_features(fr_nf PRIVATE cxx_std_17)
target_link_libraries(fr_nf PRIVATE netfilter_queue pthread)
