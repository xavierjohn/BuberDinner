# Buber Dinner API
- [Buber Dinner API](#buber-dinner-api)
  - [Auth](#auth)
  - [Register](#register)
    - [Register Request](#register-request)
    - [Register Response](#register-response)
  -[Login](#login)
    -[Login Request](#login-request)
    -[Login Response](#login-response)

## Auth

### Register

```js
POST {{host}}/auth/register
```

#### Register Request
```json
{
    "firstName":"Xavier",
    "lastName":"John",
    "email":"someone@somewhere.com",
    "password":"Amiko1232!"
}
```

#### Register Response
```js
200 OK
```

```json
{
    "id": "2e54106f-4be0-4b69-987a-8e37a8bcf10d",
    "firstName":"Xavier",
    "lastName":"John",
    "email":"someone@somewhere.com",
    "token":"asdfwe...2sfasw"
}
```
