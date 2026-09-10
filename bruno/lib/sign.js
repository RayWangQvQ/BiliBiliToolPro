const CryptoJS = require('crypto-js');
const md5 = (str) => CryptoJS.MD5(str).toString(CryptoJS.enc.Hex);

/** Convert form-urlencoded body array [{name,value},...] to plain object */
const arrayToObj = (arr) => arr.reduce((obj, item) => { obj[item.name] = item.value; return obj; }, {});

/** Replace {{placeholder}} with env/runtime vars */
const replacePlaceholders = (obj, bru) => {
  for (const key in obj) {
    if (typeof obj[key] === 'string') {
      obj[key] = obj[key].replace(/\{\{(.*?)\}\}/g, (match, name) => {
        return bru.getVar(name) || bru.getEnvVar(name) || match;
      });
    }
  }
};

/** Build sorted query string from params object */
const buildSortedQuery = (params) =>
  Object.keys(params).sort()
    .map(key => `${encodeURIComponent(key)}=${encodeURIComponent(params[key])}`)
    .join('&');

/** Calculate app sign: update ts, remove old sign, then md5(sorted_params + appsec) */
function calcSign(params, appsec) {
  params.ts = Math.floor(Date.now() / 1000).toString();
  delete params.sign;
  const query = buildSortedQuery(params);
  console.log("sorted params: " + query);
  return md5(query + appsec);
}

/** Get the matching appSec for the appkey in params */
function getAppSec(params, bru) {
  const appKeyGuest = bru.getEnvVar("appKeyGuest");
  if (params.appkey === appKeyGuest) {
    return bru.getEnvVar("appSecGuest");
  }
  return bru.getEnvVar("appSec");
}

/**
 * Collect params from request body or query, resolve placeholders, calc sign.
 * Returns true if sign was calculated, false if skipped.
 */
function signRequest(req, bru) {
  const rawBody = req.getBody();
  const isArrayBody = Array.isArray(rawBody);
  const bodyObj = isArrayBody ? arrayToObj(rawBody) : rawBody;

  const url = req.getUrl();
  const queryStr = url.includes('?') ? url.split('?')[1] : '';
  const queryObj = {};
  if (queryStr) {
    queryStr.split('&').forEach(pair => {
      const [k, ...rest] = pair.split('=');
      queryObj[decodeURIComponent(k)] = decodeURIComponent(rest.join('='));
    });
  }

  const params = (bodyObj && bodyObj.hasOwnProperty('sign')) ? bodyObj
               : (queryObj.hasOwnProperty('sign')) ? queryObj
               : null;

  if (!params) return false;

  replacePlaceholders(params, bru);

  // ts should always use current timestamp for each request run.
  // If ts is still a placeholder (e.g. {{ts}}), fill it before unresolved check.
  if (typeof params.ts === 'string' && /\{\{.*?\}\}/.test(params.ts)) {
    params.ts = Math.floor(Date.now() / 1000).toString();
  }

  // Check if any param still has unresolved {{...}} placeholders
  // If so, skip — a later runtime script will handle it
  for (const key in params) {
    // sign will be regenerated in calcSign, so placeholder here is harmless.
    if (key === 'sign') continue;
    if (typeof params[key] === 'string' && /\{\{.*?\}\}/.test(params[key])) {
      console.log(`sign skipped: param '${key}' has unresolved placeholder: ${params[key]}`);
      return false;
    }
  }

  const appsec = getAppSec(params, bru);
  const sign = calcSign(params, appsec);
  console.log("appkey: " + params.appkey + ", appsec: " + appsec);

  bru.setVar("ts", params.ts);
  bru.setVar("sign", sign);
  console.log("ts: " + params.ts + ", sign: " + sign);
  return true;
}

module.exports = { arrayToObj, replacePlaceholders, buildSortedQuery, calcSign, getAppSec, signRequest };
