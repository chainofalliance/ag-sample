#!/bin/bash

ROOTDIR=$1
CONFDIR=$2
export ENV=$3
IFS=',' read -r -a TEST_MODULES <<< "$4"

DEPLOYMENTS_KEY='deployments:'

if [[ "$ENV" == "prod" ]]; then
    export MAIN_MODULE="main"
else
    export MAIN_MODULE="main"
fi

. ${CONFDIR}/${ENV}.env

envsubst < ${CONFDIR}/template/config_base_template.yml > ${ROOTDIR}/chromia.yml

if [ -z ${TEST_MODULES} ]; then
    for d in ${ROOTDIR}/src/ag/tests/*/ ; do
        MODULE=$(basename $d)
        if [[ "$MODULE" == *"tests"* ]]; then
            TEST_MODULES+=($MODULE)
        fi
    done
fi

for module in "${TEST_MODULES[@]}"; do
    printf "    - ag.tests.${module}\n" >> ${ROOTDIR}/chromia.yml
done

if [ ! -z ${CONTAINER_ID_TESTNET} ]; then
    echo -e "\n$DEPLOYMENTS_KEY" >> ${ROOTDIR}/chromia.yml
    envsubst < ${CONFDIR}/template/config_testnet_template.yml >> ${ROOTDIR}/chromia.yml
    if [ -z ${BRID_TESTNET} ]; then
        ex -snc '$-1,$d|x' ${ROOTDIR}/chromia.yml
    fi
fi
