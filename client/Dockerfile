FROM unityci/editor:ubuntu-6000.0.40f1-webgl-3

ARG DEPLOY_KEY
RUN test -n "$DEPLOY_KEY" || (echo "DEPLOY_KEY not set" && false)
ENV DEPLOY_KEY $DEPLOY_KEY

RUN apt-get update && \
    apt-get install -y \
    jq \
    gettext-base \
    dos2unix \
    ca-certificates \
    curl \
    gnupg \
    git 

RUN install -m 0755 -d /etc/apt/keyrings
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
RUN chmod a+r /etc/apt/keyrings/docker.gpg
RUN echo \
    "deb [arch="$(dpkg --print-architecture)" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
    "$(. /etc/os-release && echo "$VERSION_CODENAME")" stable" | \
    tee /etc/apt/sources.list.d/docker.list > /dev/null
RUN apt-get update && \
    apt-get install -y \
    docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

RUN mkdir -p ~/.ssh
RUN ssh-keyscan github.com >> ~/.ssh/known_hosts
RUN ssh-keyscan 13.48.149.47 >> ~/.ssh/known_hosts
RUN echo "$DEPLOY_KEY" >> ~/.ssh/id_rsa
RUN chmod 600 ~/.ssh/id_rsa

ENV IS_CI=true
ENV CI_TARGET=client

RUN mkdir /app
WORKDIR /app

RUN git config --global --add safe.directory /app

ENTRYPOINT [ "/app/scripts/ci.sh" ]
