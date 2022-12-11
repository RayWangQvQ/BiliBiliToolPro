'''
1 9 11 11 1 bili_task_get_cookie.py
手动运行，查看日志，并使用手机B站app扫描日志中二维码，注意，只能修改第一个cookie
如果产生错误，重新运行并用手机扫描二维码
有可能识别不出来二维码，我测试了几次都能识别

默认环境变量存放位置为/ql/data/config/env.sh
可以自己通过docker命令进入容器查找这个文件位置。docker exec -it qinglong /bin/bash,进入青龙容器，然后查找一下这个文件位置
filename = '../config/env.sh'
'''

import qrcode
import requests
import json
import time
import os

filename = '/ql/data/config/env.sh'

url_get = 'http://passport.bilibili.com/x/passport-login/web/qrcode/generate'
headers = {
    "user-agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 Edg/108.0.1462.42"
    }
session = requests.session()
response = session.get(url_get, headers=headers)
json_data = json.loads(response.text)
qr_data = json_data['data']['url']
qr_code = json_data['data']['qrcode_key']
# print(qr_data)
#  img = qrcode.make(qr_data)
#  img.save('../upload/B.png')
# 生成二维码，并且打印，只有invert是True手机才能识别，默认的打印识别不出来
qr = qrcode.QRCode()
qr.add_data(qr_data)
qr.print_ascii(invert=True)

url_get_2 = f'http://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qr_code}&source=main_mini'
refresh_token = ''
# 尝试次数
try_time = 8
while True:
    try_time -= 1
    if not try_time:
        print('一直没有扫码，退出登录！')
        exit(1)
    response = session.get(url_get_2, headers=headers)
    json_data = json.loads(response.text)
    response_data_2 = json_data['data']
    if response_data_2['code'] == 0:
        try_time += 5
        refresh_token = response_data_2['refresh_token']
        print(response_data_2, end='')
    if response_data_2['message'] == '二维码已失效':
        print(response_data_2['message'])
        print('-' * 20)
        break
    print(response_data_2['message'])
    print('-' * 20)
    time.sleep(5)
session.get('https://api.bilibili.com/x/web-interface/nav')
cookies = requests.utils.dict_from_cookiejar(session.cookies)
lst = []
for item in cookies.items():
    lst.append(f"{item[0]}={item[1]}")

cookie_str = ';'.join(lst)
print('=' * 20)
print(cookie_str)
print('=' * 20)
# 修改环境变量
with open(filename, 'r') as f:
    lines = f.readlines()

flag = True
with open(filename, 'w') as f:
    for l in lines:
        if 'Ray_BiliBiliCookies__1' in l:
            flag = False
            l = f'export Ray_BiliBiliCookies__1="{cookie_str}"\n'
            print(l)
        f.write(l)
    if flag:
        flag = False
        l = f'export Ray_BiliBiliCookies__1="{cookie_str}"\n'
        print(l)
        f.write(l)
os.popen(f'source {filename}')
