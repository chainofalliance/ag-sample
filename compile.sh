#!/bin/bash

compiled_dir="server/Grpc"

current_dir=$(pwd)
protos_dir="${current_dir}/protos"

rm -rf "${current_dir}/${compiled_dir}/*"

files=""
for d in $protos_dir/* ; do
    files="${files} -f ${d##*/} "
done

export MSYS_NO_PATHCONV=1 && docker run \
    -v "${current_dir}/${compiled_dir}":/out \
    -v "${protos_dir}":/defs \
    namely/protoc-all \
    ${files} \
    -o "/out" \
    -l csharp

cp -r "${current_dir}/${compiled_dir}/." "${current_dir}/client/TicTacToe/Assets/Scripts/Grpc"