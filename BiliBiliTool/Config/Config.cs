using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BiliBiliTool.Config
{
    public class Config
    {
        /**
         * 每日设定的投币数 [0,5]
         */
        private int numberOfCoins;

        /**
         * 投币时是否点赞 [0,1]
         */
        private int selectLike;

        /**
         * 观看时是否分享 [0,1]
         */
        private int watchAndShare;

        /**
         * 年度大会员自动充电[false,true]
         */
        private bool monthEndAutoCharge;

        /**
         * 执行客户端操作时的平台 [ios,android]
         */
        private String devicePlatform;

        public String getDevicePlatform()
        {
            return devicePlatform;
        }


        private static Config CONFIG = new Config();

        public static Config getInstance()
        {
            return CONFIG;
        }

        public int getSelectLike()
        {
            return selectLike;
        }

        public int getWatchAndShare()
        {
            return watchAndShare;
        }

        public Config()
        {
        }


        public bool isMonthEndAutoCharge()
        {
            return monthEndAutoCharge;
        }

        public void setNumberOfCoins(int numberOfCoins)
        {
            this.numberOfCoins = numberOfCoins;
        }

        public int getNumberOfCoins()
        {
            return numberOfCoins;
        }


        public override String ToString()
        {
            return "Config{" +
                    "numberOfCoins=" + numberOfCoins +
                    ", selectLike=" + selectLike +
                    ", watchAndShare=" + watchAndShare +
                    ", monthEndAutoCharge=" + monthEndAutoCharge +
                    ", devicePlatform='" + devicePlatform + '\'' +
                    '}';
        }

        public String outputConfig()
        {
            String outputConfig = "您设置的每日投币数量为: ";
            outputConfig += numberOfCoins;

            if (selectLike == 1)
            {
                outputConfig += " 投币时是否点赞: " + "是";
            }
            else
            {
                outputConfig += " 投币时是否点赞: " + "否";
            }


            return outputConfig + " 执行app客户端操作的系统是: " + devicePlatform;
        }

        /**
         * 读取配置文件 src/main/resources/config.json
         */
        public void configInit(ILogger logger)
        {
            //String configJson = LoadJsonFromResources.loadJSONFromAsset();
            //Config.CONFIG = new Gson().fromJson(configJson, Config.class);
            logger.LogInformation("----Init ConfigFile Successful----");
            logger.LogInformation(Config.getInstance().outputConfig());
        }
    }

}
