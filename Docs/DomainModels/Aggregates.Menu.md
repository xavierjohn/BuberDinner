# Domain Models

## Menu

```csharp
class Menu
{
    Menu Create();
    void AddDinner(Dinner dinner);
    void RemoveDinner(Dinner dinner);
    void UpdateSection(MenuSelection section);
}
```

```json
{
    "id" : "000000-000-0000-000000000",
    "name": "Yummy menu",
    "description" : "A menu with yummy food",
    "averageRating" : 4.5,
    "sections" : [
        {
            "id": "000000-000-0000-000000000",
            "name" : "Appetizers",
            "description" : "Starters",
            "items" : [
                {
                    "id": "000000-000-0000-000000000",
                    "name" : "Garlic fries",
                    "description" : "Deep fried french fries with garlic seasoning",
                    "price" : 7.00
                }
            ]
        }
    ],
    "hostId" : "000000-000-0000-000000000",
    "dinnerIds" : [
        "000000-000-0000-000000000",
    ],
    "menuReviewIds": [
        "000000-000-0000-000000000",
    ],
    "createdOn" : "2022-01-01T:00:00:00.000000Z",
    "updatedOn" : "2022-01-01T:00:00:00.000000Z",
}

```
