#!/usr/bin/env bash
set -e
set -u
set -o pipefail

echo '  ____               ____    _   _____           _  '
echo ' |  _ \ __ _ _   _  | __ ) _| |_|_   _|__   ___ | | '
echo ' | |_) / _` | | | | |  _ \(_) (_) | |/ _ \ / _ \| | '
echo ' |  _ < (_| | |_| | | |_) | | | | | | (_) | (_) | | '
echo ' |_| \_\__,_|\__, | |____/|_|_|_| |_|\___/ \___/|_| '
echo '             |___/                                  '

# ------------vars-----------、

# --------------------------

# read params from init cmd


read_var_from_user() {
    eval $invocation

    # host
    if [ -z "$host" ]; then
        read -p "请输入域名(如demo.test.tk):" host
    else
        say "域名: $host"
    fi

    # cert
    if [ -z "$certMode" ]; then
        read -p "请输入证书模式(1.Caddy自动颁发；2.使用现有证书。默认1):" certMode
        if [ -z "$certMode" ]; then
            certMode="1"
        fi
    fi

    if [ "$certMode" == "1" ]; then
        # say "certMode: $certMode（由Caddy自动颁发）"
        say_warning "自动颁发证书需要开放80端口给Caddy使用，请确保80端口开放且未被占用"
        httpPort="80"
        # email
        if [ -z "$mail" ]; then
            read -p "请输入邮箱(如test@qq.com):" mail
        else
            say "邮箱: $mail"
        fi
    else
        # say "certMode: 2（使用现有证书）"
        autoHttps="auto_https disable_certs"
        if [ -z "$certKeyFile" ]; then
            read -p "请输入证书key文件路径:" certKeyFile
        else
            say "证书key: $certKeyFile"
        fi

        if [ -z "$certFile" ]; then
            read -p "请输入证书文件路径:" certFile
        else
            say "证书文件: $certFile"
        fi
    fi

    # port
    if [ -z "$httpPort" ]; then
        if [ $certMode == "2" ]; then
            say "使用现有证书模式允许使用非80的http端口"
            read -p "请输入Caddy的http端口(如8080, 默认80):" httpPort
            if [ -z "$httpPort" ]; then
                httpPort="80"
            fi
        else
            httpPort="80"
            say "Http端口: $httpPort"
        fi
    else
        say "httpPort: $httpPort"
    fi

    if [ -z "$httpsPort" ]; then
        read -p "请输入https端口(如8043, 默认443):" httpsPort
        if [ -z "$httpsPort" ]; then
            httpsPort="443"
        fi
    else
        say "Https端口: $httpsPort"
    fi

    if [ -z "$user" ]; then
        read -p "请输入节点用户名(如zhangsan):" user
    else
        say "节点用户名: $user"
    fi

    if [ -z "$pwd" ]; then
        read -p "请输入节点密码(如1qaz@wsx):" pwd
    else
        say "节点密码: $pwd"
    fi

    if [ -z "$fakeHost" ]; then
        read -p "请输入伪装站点地址(默认$fakeHostDefault):" fakeHost
        if [ -z "$fakeHost" ]; then
            fakeHost=$fakeHostDefault
        fi
    else
        say "伪装站点地址: $fakeHost"
    fi
}

main() {
    dotnet publish --configuration Release --self-contained false -o ./bin/Publish/net5-dependent
    echo "dotnet Ray.BiliBiliTool.Console.dll" > ./bin/Publish/net5-dependent/start.bat
}

main
